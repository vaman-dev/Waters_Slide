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

        [Header("Runtime")]
        [SerializeField] private PlayerSlideRuntime runtime = new PlayerSlideRuntime();

        public PlayerSlideRuntime Runtime => runtime;
        public PlayerSlideData PlayerData => playerData;

        private void Reset()
        {
            splineFollowMotor = GetComponent<SplineFollowMotor>();
        }

        private void Awake()
        {
            if (splineFollowMotor == null)
                splineFollowMotor = GetComponent<SplineFollowMotor>();
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

            UpdateSpeed(Time.deltaTime);

            if (runtime.CurrentState == PlayerSlideState.FollowingSpline)
            {
                splineFollowMotor.TickFollow(runtime, playerData, Time.deltaTime);

                if (runtime.CurrentT >= 1f)
                {
                    OnReachedSplineEnd();
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