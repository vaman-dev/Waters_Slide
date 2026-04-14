using UnityEngine;
using WaterSlide.Player;
using WaterSlide.Spline;

namespace WaterSlide.CameraSystem
{
    public class CameraSpeedFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSlideController playerSlideController;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform cameraRoot;

        [Header("Base Local Pose")]
        [SerializeField] private Vector3 baseLocalPosition = Vector3.zero;

        [Header("FOV")]
        [SerializeField] private float minFOV = 55f;
        [SerializeField] private float maxFOV = 60f;
        [SerializeField] private float fovLerpSpeed = 10f;

        [Header("Rotation")]
        [SerializeField] private float rotationLerpSpeed = 6f;

        [Header("Downhill Look")]
        [SerializeField] private float maxDownhillPitch = 18f;
        [SerializeField] private float landingPitchAmount = 8f;
        [SerializeField] private float landingPitchRecoverSpeed = 5f;

        [SerializeField]
        private AnimationCurve downhillPitchCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0.15f),
                new Keyframe(1f, 1f)
            );

        [Header("Jump Alignment")]
        [SerializeField] private bool enableJumpAlignment = true;
        [SerializeField] private float jumpRotationLerpSpeed = 4f;

        [Header("Jump Mouse Look")]
        [SerializeField] private bool enableJumpMouseLook = true;
        [SerializeField] private float jumpLookSensitivityX = 0.08f;
        [SerializeField] private float jumpLookSensitivityY = 0.08f;
        [SerializeField] private float maxJumpLookYaw = 35f;
        [SerializeField] private float maxJumpLookPitch = 20f;
        [SerializeField] private float jumpLookReturnSpeed = 4f;

        [Header("Camera Shake")]
        [SerializeField] private bool enableShake = true;
        [SerializeField] private float shakeStartSpeed = 12f;
        [SerializeField] private float shakeMaxSpeed = 25f;
        [SerializeField] private float maxShakePosition = 0.025f;
        [SerializeField] private float maxShakeRotation = 0.8f;
        [SerializeField] private float shakeFrequency = 18f;

        [SerializeField]
        private AnimationCurve shakeBySlopeCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0.1f),
                new Keyframe(1f, 1f)
            );

        private float landingPitchOffset;
        private bool wasJumpingLastFrame;
        private float shakeTime;

        private float jumpLookYaw;
        private float jumpLookPitch;

        private void Reset()
        {
            cameraRoot = transform;
            targetCamera = GetComponent<Camera>();

            if (targetCamera == null)
                targetCamera = GetComponentInChildren<Camera>();

            playerSlideController = GetComponentInParent<PlayerSlideController>();
            inputReader = GetComponentInParent<PlayerInputReader>();
        }

        private void Awake()
        {
            if (cameraRoot == null)
                cameraRoot = transform;

            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            if (targetCamera == null)
                targetCamera = GetComponentInChildren<Camera>();

            if (playerSlideController == null)
                playerSlideController = GetComponentInParent<PlayerSlideController>();

            if (inputReader == null)
                inputReader = GetComponentInParent<PlayerInputReader>();

            if (cameraRoot != null)
                baseLocalPosition = cameraRoot.localPosition;
        }

        private void LateUpdate()
        {
            if (playerSlideController == null || playerSlideController.Runtime == null)
                return;

            UpdateLandingState();
            UpdateJumpLook();
            UpdateFOV();
            UpdateCameraTransform();
        }

        private void UpdateLandingState()
        {
            bool isJumpingNow = playerSlideController.Runtime.CurrentState == PlayerSlideState.Jumping;

            if (wasJumpingLastFrame && !isJumpingNow)
            {
                landingPitchOffset = landingPitchAmount;
            }

            wasJumpingLastFrame = isJumpingNow;

            landingPitchOffset = Mathf.Lerp(
                landingPitchOffset,
                0f,
                landingPitchRecoverSpeed * Time.deltaTime
            );
        }

        private void UpdateJumpLook()
        {
            bool isJumping = IsJumping();

            if (enableJumpMouseLook && isJumping && inputReader != null)
            {
                Vector2 look = inputReader.LookInput;

                jumpLookYaw += look.x * jumpLookSensitivityX;
                jumpLookPitch -= look.y * jumpLookSensitivityY;

                jumpLookYaw = Mathf.Clamp(jumpLookYaw, -maxJumpLookYaw, maxJumpLookYaw);
                jumpLookPitch = Mathf.Clamp(jumpLookPitch, -maxJumpLookPitch, maxJumpLookPitch);
            }
            else
            {
                jumpLookYaw = Mathf.Lerp(jumpLookYaw, 0f, jumpLookReturnSpeed * Time.deltaTime);
                jumpLookPitch = Mathf.Lerp(jumpLookPitch, 0f, jumpLookReturnSpeed * Time.deltaTime);
            }
        }

        private void UpdateFOV()
        {
            if (targetCamera == null)
                return;

            float speed = playerSlideController.Runtime.CurrentSpeed;
            float maxSpeed = Mathf.Max(0.01f, playerSlideController.PlayerData.maxSpeed);

            float t = Mathf.InverseLerp(0f, maxSpeed, speed);
            float targetFOVValue = Mathf.Lerp(minFOV, maxFOV, t);

            targetCamera.fieldOfView = Mathf.Lerp(
                targetCamera.fieldOfView,
                targetFOVValue,
                fovLerpSpeed * Time.deltaTime
            );
        }

        private void UpdateCameraTransform()
        {
            if (cameraRoot == null)
                return;

            Quaternion targetRotation = GetTargetRotation();
            Vector3 shakePositionOffset;
            Quaternion shakeRotationOffset;
            GetShakeOffsets(out shakePositionOffset, out shakeRotationOffset);

            cameraRoot.localPosition = Vector3.Lerp(
                cameraRoot.localPosition,
                baseLocalPosition + shakePositionOffset,
                rotationLerpSpeed * Time.deltaTime
            );

            float currentLerpSpeed = IsJumping() ? jumpRotationLerpSpeed : rotationLerpSpeed;

            cameraRoot.rotation = Quaternion.Slerp(
                cameraRoot.rotation,
                targetRotation * shakeRotationOffset,
                currentLerpSpeed * Time.deltaTime
            );
        }

        private Quaternion GetTargetRotation()
        {
            var runtime = playerSlideController.Runtime;
            if (runtime == null || runtime.CurrentSlide == null)
                return cameraRoot.rotation;

            float slope01 = GetSlopeNormalized01();
            float downhillPitchWeight = downhillPitchCurve.Evaluate(slope01);
            float downhillPitch = maxDownhillPitch * downhillPitchWeight;

            if (enableJumpAlignment && runtime.CurrentState == PlayerSlideState.Jumping)
            {
                JumpTransferMotor jumpMotor = playerSlideController.GetComponent<JumpTransferMotor>();

                if (jumpMotor != null && jumpMotor.TargetSlide != null)
                {
                    Vector3 forward = jumpMotor.TargetSlide.EvaluateTangent(jumpMotor.TargetLandingT);
                    Vector3 up = jumpMotor.TargetSlide.EvaluateUp(jumpMotor.TargetLandingT);

                    if (forward.sqrMagnitude < 0.0001f)
                        forward = cameraRoot.forward;

                    if (up.sqrMagnitude < 0.0001f)
                        up = Vector3.up;

                    Quaternion slideRotation = Quaternion.LookRotation(forward, up);

                    Quaternion slopePitchOffset = Quaternion.Euler(
                        -(downhillPitch + landingPitchOffset),
                        0f,
                        0f
                    );

                    Quaternion freeLookOffset = Quaternion.Euler(
                        jumpLookPitch,
                        jumpLookYaw,
                        0f
                    );

                    return slideRotation * slopePitchOffset * freeLookOffset;
                }
            }

            Vector3 currentForward = runtime.CurrentSlide.EvaluateTangent(runtime.CurrentT);
            Vector3 currentUp = runtime.CurrentSlide.EvaluateUp(runtime.CurrentT);

            if (currentForward.sqrMagnitude < 0.0001f)
                currentForward = cameraRoot.forward;

            if (currentUp.sqrMagnitude < 0.0001f)
                currentUp = Vector3.up;

            Quaternion currentSlideRotation = Quaternion.LookRotation(currentForward, currentUp);
            Quaternion currentPitchOffset = Quaternion.Euler(
                -(downhillPitch + landingPitchOffset),
                0f,
                0f
            );

            return currentSlideRotation * currentPitchOffset;
        }

        private bool IsJumping()
        {
            return playerSlideController != null &&
                   playerSlideController.Runtime != null &&
                   playerSlideController.Runtime.CurrentState == PlayerSlideState.Jumping;
        }

        private float GetSlopeNormalized01()
        {
            var runtime = playerSlideController.Runtime;
            if (runtime == null || runtime.CurrentSlide == null)
                return 0.5f;

            Vector3 tangent = runtime.CurrentSlide.EvaluateTangent(runtime.CurrentT);
            if (tangent.sqrMagnitude < 0.0001f)
                return 0.5f;

            tangent.Normalize();

            float downhillAmount = -tangent.y;
            return Mathf.InverseLerp(-1f, 1f, downhillAmount);
        }

        private void GetShakeOffsets(out Vector3 shakePositionOffset, out Quaternion shakeRotationOffset)
        {
            shakePositionOffset = Vector3.zero;
            shakeRotationOffset = Quaternion.identity;

            if (!enableShake || playerSlideController == null || playerSlideController.Runtime == null)
                return;

            float currentSpeed = playerSlideController.Runtime.CurrentSpeed;
            float speed01 = Mathf.InverseLerp(shakeStartSpeed, shakeMaxSpeed, currentSpeed);

            float slope01 = GetSlopeNormalized01();
            float slopeShake = shakeBySlopeCurve.Evaluate(slope01);

            float shakeAmount = speed01 * slopeShake;
            if (shakeAmount <= 0.001f)
                return;

            shakeTime += Time.deltaTime * shakeFrequency;

            float noiseX = Mathf.PerlinNoise(shakeTime, 0f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(0f, shakeTime) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(shakeTime, shakeTime) - 0.5f;

            shakePositionOffset = new Vector3(
                noiseX * 2f * maxShakePosition * shakeAmount,
                noiseY * 2f * maxShakePosition * shakeAmount,
                0f
            );

            Vector3 eulerOffset = new Vector3(
                noiseY * 2f * maxShakeRotation * shakeAmount,
                noiseX * 2f * maxShakeRotation * shakeAmount,
                noiseZ * 2f * maxShakeRotation * 0.5f * shakeAmount
            );

            shakeRotationOffset = Quaternion.Euler(eulerOffset);
        }
    }
}