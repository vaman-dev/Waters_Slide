using UnityEngine;
using WaterSlide.Data;
using WaterSlide.Spline;

namespace WaterSlide.Player
{
    public class PlayerSlideController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSlideData playerData;
        [SerializeField] private WaterSlideSpline startingSlide;
        [SerializeField] private SplineFollowMotor splineFollowMotor;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private JumpTransferMotor jumpTransferMotor;
        [SerializeField] private SlideJumpConnection[] jumpConnections;

        [Header("Runtime")]
        [SerializeField] private PlayerSlideRuntime runtime = new PlayerSlideRuntime();

        [Header("Slope Speed")]
        [SerializeField] private float baseFlatSpeed = 10f;
        [SerializeField] private float minSlideSpeed = 4f;
        [SerializeField] private float maxSlideSpeedOverride = 30f;
        [SerializeField] private float speedLerpRate = 4f;

        [Tooltip("Input is normalized slope amount from -1 to 1. -1 = steep uphill, 0 = flat, 1 = steep downhill.")]
        [SerializeField]
        private AnimationCurve slopeSpeedMultiplierCurve =
            new AnimationCurve(
                new Keyframe(0f, 0.55f),   // uphill
                new Keyframe(0.5f, 1f),    // flat
                new Keyframe(1f, 1.75f)    // downhill
            );

        [SerializeField] private float slopeInfluence = 1f;

        public PlayerSlideRuntime Runtime => runtime;
        public PlayerSlideData PlayerData => playerData;

        private void Reset()
        {
            splineFollowMotor = GetComponent<SplineFollowMotor>();
            inputReader = GetComponent<PlayerInputReader>();
            jumpTransferMotor = GetComponent<JumpTransferMotor>();
        }

        private void Awake()
        {
            if (splineFollowMotor == null)
                splineFollowMotor = GetComponent<SplineFollowMotor>();

            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();

            if (jumpTransferMotor == null)
                jumpTransferMotor = GetComponent<JumpTransferMotor>();
        }

        private void Start()
        {
            if (playerData == null)
            {
                Debug.LogError("[PlayerSlideController] PlayerSlideData is missing.", this);
                enabled = false;
                return;
            }

            if (startingSlide == null)
            {
                Debug.LogError("[PlayerSlideController] Starting slide is missing.", this);
                enabled = false;
                return;
            }

            InitializePlayer(startingSlide);
        }

        private void Update()
        {
            if (runtime.CurrentState == PlayerSlideState.Paused)
                return;

            if (runtime.CurrentState == PlayerSlideState.Finished)
                return;

            if (jumpTransferMotor != null && jumpTransferMotor.IsJumping)
            {
                jumpTransferMotor.TickJump(Time.deltaTime);

                if (!jumpTransferMotor.IsJumping)
                {
                    LandOnTargetSlide();
                }

                return;
            }

            UpdateSpeed(Time.deltaTime);

            if (runtime.CurrentState == PlayerSlideState.FollowingSpline)
            {
                TryStartJump();

                if (runtime.CurrentState == PlayerSlideState.FollowingSpline)
                {
                    splineFollowMotor.TickFollow(runtime, playerData, Time.deltaTime);

                    if (runtime.CurrentT >= 1f)
                    {
                        OnReachedSplineEnd();
                    }
                }
            }
        }

        public void InitializePlayer(WaterSlideSpline slide)
        {
            runtime.ResetRuntime(slide, playerData.startT, Mathf.Max(playerData.startSpeed, baseFlatSpeed));
            runtime.CurrentSpeed = Mathf.Clamp(runtime.CurrentSpeed, minSlideSpeed, maxSlideSpeedOverride);

            if (splineFollowMotor != null)
            {
                splineFollowMotor.SnapToSpline(runtime, playerData);
            }
        }

        private void UpdateSpeed(float deltaTime)
        {
            if (runtime.CurrentSlide == null)
                return;

            Vector3 tangent = runtime.CurrentSlide.EvaluateTangent(runtime.CurrentT);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = transform.forward;

            tangent.Normalize();

            // tangent.y < 0 => downhill, tangent.y > 0 => uphill
            float downhillAmount = -tangent.y * slopeInfluence;

            // convert from [-1,1] to [0,1] for the curve
            float curveInput = Mathf.InverseLerp(-1f, 1f, downhillAmount);
            float slopeMultiplier = slopeSpeedMultiplierCurve.Evaluate(curveInput);

            float desiredSpeed = baseFlatSpeed * slopeMultiplier;

            // Optional legacy acceleration influence kept small by design
            desiredSpeed += playerData.acceleration * deltaTime;

            float finalMaxSpeed = Mathf.Max(playerData.maxSpeed, maxSlideSpeedOverride);
            desiredSpeed = Mathf.Clamp(desiredSpeed, minSlideSpeed, finalMaxSpeed);

            runtime.CurrentSpeed = Mathf.Lerp(
                runtime.CurrentSpeed,
                desiredSpeed,
                speedLerpRate * deltaTime
            );
        }

        private void TryStartJump()
        {
            if (inputReader == null || jumpTransferMotor == null || jumpConnections == null)
                return;

            if (!inputReader.ConsumeJumpPressed())
                return;

            WaterSlideSpline currentSlide = runtime.CurrentSlide;
            if (currentSlide == null)
                return;

            for (int i = 0; i < jumpConnections.Length; i++)
            {
                SlideJumpConnection connection = jumpConnections[i];

                if (connection == null)
                    continue;

                if (!connection.IsValidForSlide(currentSlide))
                    continue;

                StartJump(connection);
                return;
            }
        }

        private void StartJump(SlideJumpConnection connection)
        {
            if (connection == null)
                return;

            Pose startPose = splineFollowMotor.GetCurrentPose(runtime, playerData);

            bool canReachTarget = connection.IsWithinJumpDistance(startPose.position);

            WaterSlideSpline landingSlide;
            float landingT;

            if (canReachTarget)
            {
                landingSlide = connection.TargetSlide;

                landingT = Mathf.Clamp01(
                    connection.TargetLandingT +
                    connection.ForwardLandingBoostT
                );
            }
            else
            {
                landingSlide = runtime.CurrentSlide;

                landingT = Mathf.Clamp01(
                    runtime.CurrentT +
                    connection.FallbackForwardOffsetT +
                    connection.ForwardLandingBoostT
                );
            }

            Pose landingPose = splineFollowMotor.GetPoseOnSlide(
                landingSlide,
                landingT,
                playerData
            );

            runtime.CurrentState = PlayerSlideState.Jumping;

            jumpTransferMotor.StartJump(
                startPose.position,
                startPose.rotation,
                landingPose.position,
                landingPose.rotation,
                landingSlide,
                landingT,
                connection.JumpDuration,
                connection.JumpHeight
            );
        }

        private void LandOnTargetSlide()
        {
            if (jumpTransferMotor == null || jumpTransferMotor.TargetSlide == null)
                return;

            runtime.CurrentSlide = jumpTransferMotor.TargetSlide;
            runtime.CurrentT = jumpTransferMotor.TargetLandingT;
            runtime.CurrentState = PlayerSlideState.FollowingSpline;

            runtime.CurrentSpeed = Mathf.Min(
                runtime.CurrentSpeed + 2f,
                Mathf.Max(playerData.maxSpeed, maxSlideSpeedOverride)
            );

            if (splineFollowMotor != null)
            {
                splineFollowMotor.SnapToSpline(runtime, playerData);
            }
        }

        private void OnReachedSplineEnd()
        {
            if (playerData.stopAtSplineEnd)
            {
                runtime.CurrentSpeed = 0f;
                runtime.CurrentState = PlayerSlideState.Finished;
            }
        }

        public void PausePlayer()
        {
            runtime.CurrentState = PlayerSlideState.Paused;
        }

        public void ResumePlayer()
        {
            if (runtime.CurrentSlide == null)
                return;

            runtime.CurrentState = PlayerSlideState.FollowingSpline;
        }

        public void SetSpeed(float newSpeed)
        {
            float finalMaxSpeed = Mathf.Max(playerData.maxSpeed, maxSlideSpeedOverride);
            runtime.CurrentSpeed = Mathf.Clamp(newSpeed, minSlideSpeed, finalMaxSpeed);
        }

        public void SetSlide(WaterSlideSpline newSlide, float startT = 0f, bool snapImmediately = true)
        {
            if (newSlide == null)
                return;

            runtime.CurrentSlide = newSlide;
            runtime.CurrentT = Mathf.Clamp01(startT);
            runtime.CurrentState = PlayerSlideState.FollowingSpline;

            if (snapImmediately && splineFollowMotor != null)
            {
                splineFollowMotor.SnapToSpline(runtime, playerData);
            }
        }
    }
}