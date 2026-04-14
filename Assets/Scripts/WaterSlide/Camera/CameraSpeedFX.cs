using UnityEngine;
using WaterSlide.Player;

namespace WaterSlide.CameraSystem
{
    public class CameraSpeedFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSlideController playerSlideController;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform cameraRoot;

        [Header("Base Local Pose")]
        [SerializeField] private Vector3 baseLocalPosition = Vector3.zero;

        [Header("Speed FOV")]
        [SerializeField] private float minSpeedForEffect = 4f;
        [SerializeField] private float maxSpeedForEffect = 25f;
        [SerializeField] private float minFOV = 60f;
        [SerializeField] private float maxFOV = 78f;
        [SerializeField] private float fovLerpSpeed = 6f;

        [Header("Slope Camera Offset")]
        [SerializeField] private float maxForwardOffset = 0.45f;
        [SerializeField] private float maxDownwardOffset = 0.15f;
        [SerializeField] private float positionLerpSpeed = 6f;

        [Tooltip("Input is normalized slope amount from -1 to 1. -1 = steep uphill, 0 = flat, 1 = steep downhill.")]
        [SerializeField]
        private AnimationCurve slopeCameraCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0.35f),
                new Keyframe(1f, 1f)
            );

        [Tooltip("Additional FOV response from slope. -1 uphill, 0 flat, 1 downhill mapped to 0..1 for curve.")]
        [SerializeField]
        private AnimationCurve slopeFOVCurve =
            new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.5f, 0.45f),
                new Keyframe(1f, 1f)
            );

        [Header("High Speed Downhill Shake")]
        [SerializeField] private bool enableShake = true;
        [SerializeField] private float shakeStartSpeed = 14f;
        [SerializeField] private float shakeMaxSpeed = 28f;
        [SerializeField] private float maxShakePosition = 0.05f;
        [SerializeField] private float maxShakeRotation = 1.5f;
        [SerializeField] private float shakeFrequency = 18f;
        [SerializeField]
        private AnimationCurve shakeBySlopeCurve =
            new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0.15f),
                new Keyframe(1f, 1f)
            );

        private float shakeTime;

        private void Reset()
        {
            if (cameraRoot == null)
                cameraRoot = transform;

            if (targetCamera == null)
                targetCamera = GetComponentInChildren<Camera>();

            if (playerSlideController == null)
                playerSlideController = GetComponentInParent<PlayerSlideController>();
        }

        private void Awake()
        {
            if (cameraRoot == null)
                cameraRoot = transform;

            if (targetCamera == null)
                targetCamera = GetComponentInChildren<Camera>();

            if (playerSlideController == null)
                playerSlideController = GetComponentInParent<PlayerSlideController>();

            if (cameraRoot != null)
                baseLocalPosition = cameraRoot.localPosition;
        }

        private void LateUpdate()
        {
            if (playerSlideController == null || playerSlideController.Runtime == null)
                return;

            float currentSpeed = playerSlideController.Runtime.CurrentSpeed;

            float speed01 = Mathf.InverseLerp(
                minSpeedForEffect,
                maxSpeedForEffect,
                currentSpeed
            );

            float slope01 = GetSlopeNormalized01();

            UpdateFOV(speed01, slope01);
            UpdateCameraPosition(speed01, slope01, currentSpeed);
        }

        private float GetSlopeNormalized01()
        {
            if (playerSlideController == null || playerSlideController.Runtime == null)
                return 0.5f;

            var runtime = playerSlideController.Runtime;
            if (runtime.CurrentSlide == null)
                return 0.5f;

            Vector3 tangent = runtime.CurrentSlide.EvaluateTangent(runtime.CurrentT);
            if (tangent.sqrMagnitude < 0.0001f)
                return 0.5f;

            tangent.Normalize();

            float downhillAmount = -tangent.y;
            return Mathf.InverseLerp(-1f, 1f, downhillAmount);
        }

        private void UpdateFOV(float speed01, float slope01)
        {
            if (targetCamera == null)
                return;

            float speedDrivenFOV = Mathf.Lerp(minFOV, maxFOV, speed01);
            float slopeDrivenBlend = slopeFOVCurve.Evaluate(slope01);
            float finalTargetFOV = Mathf.Lerp(minFOV, speedDrivenFOV, slopeDrivenBlend);

            targetCamera.fieldOfView = Mathf.Lerp(
                targetCamera.fieldOfView,
                finalTargetFOV,
                fovLerpSpeed * Time.deltaTime
            );
        }

        private void UpdateCameraPosition(float speed01, float slope01, float currentSpeed)
        {
            if (cameraRoot == null)
                return;

            float slopeCameraAmount = slopeCameraCurve.Evaluate(slope01);

            float forwardOffset = maxForwardOffset * speed01 * slopeCameraAmount;
            float downwardOffset = maxDownwardOffset * speed01 * slopeCameraAmount;

            Vector3 targetLocalPosition = baseLocalPosition + new Vector3(
                0f,
                -downwardOffset,
                forwardOffset
            );

            Vector3 shakePositionOffset = Vector3.zero;
            Vector3 shakeRotationOffset = Vector3.zero;

            if (enableShake)
            {
                float shakeSpeed01 = Mathf.InverseLerp(shakeStartSpeed, shakeMaxSpeed, currentSpeed);
                float shakeSlope01 = shakeBySlopeCurve.Evaluate(slope01);
                float shakeAmount = shakeSpeed01 * shakeSlope01;

                if (shakeAmount > 0.001f)
                {
                    shakeTime += Time.deltaTime * shakeFrequency;

                    float noiseX = Mathf.PerlinNoise(shakeTime, 0f) - 0.5f;
                    float noiseY = Mathf.PerlinNoise(0f, shakeTime) - 0.5f;
                    float noiseZ = Mathf.PerlinNoise(shakeTime, shakeTime) - 0.5f;

                    shakePositionOffset = new Vector3(
                        noiseX * 2f * maxShakePosition * shakeAmount,
                        noiseY * 2f * maxShakePosition * shakeAmount,
                        0f
                    );

                    shakeRotationOffset = new Vector3(
                        noiseY * 2f * maxShakeRotation * shakeAmount,
                        noiseX * 2f * maxShakeRotation * shakeAmount,
                        noiseZ * 2f * maxShakeRotation * 0.5f * shakeAmount
                    );
                }
            }

            Vector3 finalTargetLocalPosition = targetLocalPosition + shakePositionOffset;

            cameraRoot.localPosition = Vector3.Lerp(
                cameraRoot.localPosition,
                finalTargetLocalPosition,
                positionLerpSpeed * Time.deltaTime
            );

            Quaternion targetRotation = Quaternion.Euler(shakeRotationOffset);

            cameraRoot.localRotation = Quaternion.Slerp(
                cameraRoot.localRotation,
                targetRotation,
                positionLerpSpeed * Time.deltaTime
            );
        }
    }
}