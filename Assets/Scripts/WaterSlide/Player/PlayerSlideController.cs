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
            runtime.ResetRuntime(slide, playerData.startT, playerData.startSpeed);

            if (splineFollowMotor != null)
            {
                splineFollowMotor.SnapToSpline(runtime, playerData);
            }
        }

        private void UpdateSpeed(float deltaTime)
        {
            float targetSpeed = runtime.CurrentSpeed + playerData.acceleration * deltaTime;
            runtime.CurrentSpeed = Mathf.Clamp(targetSpeed, 0f, playerData.maxSpeed);
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
            if (connection == null || connection.TargetSlide == null)
                return;

            Pose startPose = splineFollowMotor.GetCurrentPose(runtime, playerData);
            Pose landingPose = splineFollowMotor.GetPoseOnSlide(connection.TargetSlide, connection.TargetLandingT, playerData);

            runtime.CurrentState = PlayerSlideState.Jumping;

            jumpTransferMotor.StartJump(
                startPose.position,
                startPose.rotation,
                landingPose.position,
                landingPose.rotation,
                connection.TargetSlide,
                connection.TargetLandingT,
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
            runtime.CurrentSpeed = Mathf.Clamp(newSpeed, 0f, playerData.maxSpeed);
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