using UnityEngine;
using WaterSlide.Spline;

namespace WaterSlide.Player
{
    public class JumpTransferMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform followTarget;

        private bool isJumping;
        private float elapsedTime;
        private float duration;
        private float height;

        private Vector3 startPosition;
        private Vector3 endPosition;

        private Quaternion startRotation;
        private Quaternion endRotation;

        private WaterSlideSpline targetSlide;
        private float targetLandingT;

        public bool IsJumping => isJumping;
        public WaterSlideSpline TargetSlide => targetSlide;
        public float TargetLandingT => targetLandingT;

        private void Awake()
        {
            if (followTarget == null)
                followTarget = transform;
        }

        public void StartJump(
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 toPosition,
            Quaternion toRotation,
            WaterSlideSpline landingSlide,
            float landingT,
            float jumpDuration,
            float jumpHeight)
        {
            startPosition = fromPosition;
            endPosition = toPosition;

            startRotation = fromRotation;
            endRotation = toRotation;

            targetSlide = landingSlide;
            targetLandingT = Mathf.Clamp01(landingT);

            duration = Mathf.Max(0.01f, jumpDuration);
            height = jumpHeight;

            elapsedTime = 0f;
            isJumping = true;
        }

        public void TickJump(float deltaTime)
        {
            if (!isJumping || followTarget == null)
                return;

            elapsedTime += deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            Vector3 linearPos = Vector3.Lerp(startPosition, endPosition, t);
            float arcOffset = 4f * height * t * (1f - t);

            followTarget.position = linearPos + Vector3.up * arcOffset;
            followTarget.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            if (t >= 1f)
            {
                isJumping = false;
            }
        }
    }
}