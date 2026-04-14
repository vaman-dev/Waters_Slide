using UnityEngine;

namespace WaterSlide.Spline
{
    public class SlideJumpConnection : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private WaterSlideSpline sourceSlide;

        [Header("Target")]
        [SerializeField] private WaterSlideSpline targetSlide;
        [SerializeField, Range(0f, 1f)] private float targetLandingT = 0.05f;

        [Header("Arc")]
        [SerializeField] private float jumpDuration = 1.0f;
        [SerializeField] private float jumpHeight = 4.0f;

        [Header("Distance Constraint")]
        [SerializeField] private float maxJumpDistance = 25f;

        [Header("Fallback / Force")]
        [SerializeField] private float forwardLandingBoostT = 0.08f;
        [SerializeField] private float fallbackForwardOffsetT = 0.05f;

        [Header("Gizmo")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private int arcResolution = 20;

        public WaterSlideSpline SourceSlide => sourceSlide;
        public WaterSlideSpline TargetSlide => targetSlide;
        public float TargetLandingT => targetLandingT;
        public float JumpDuration => jumpDuration;
        public float JumpHeight => jumpHeight;
        public float ForwardLandingBoostT => forwardLandingBoostT;
        public float FallbackForwardOffsetT => fallbackForwardOffsetT;

        public bool IsValidForSlide(WaterSlideSpline currentSlide)
        {
            return sourceSlide != null && currentSlide == sourceSlide;
        }

        public bool IsWithinJumpDistance(Vector3 currentPosition)
        {
            if (targetSlide == null)
                return false;

            Vector3 targetPos = targetSlide.EvaluatePosition(targetLandingT);
            float distance = Vector3.Distance(currentPosition, targetPos);

            return distance <= maxJumpDistance;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || sourceSlide == null)
                return;

            float startT = 0.95f;
            Vector3 startPos = sourceSlide.EvaluatePosition(startT);

            Vector3 targetPos = targetSlide != null
                ? targetSlide.EvaluatePosition(targetLandingT)
                : startPos;

            float distance = Vector3.Distance(startPos, targetPos);
            bool canReach = targetSlide != null && distance <= maxJumpDistance;

            Vector3 endPos;

            if (canReach)
            {
                // NORMAL jump → next slide + forward force
                float boostedT = Mathf.Clamp01(targetLandingT + forwardLandingBoostT);
                endPos = targetSlide.EvaluatePosition(boostedT);
            }
            else
            {
                // FALLBACK → same slide + forward force
                float fallbackT = Mathf.Clamp01(startT + fallbackForwardOffsetT + forwardLandingBoostT);
                endPos = sourceSlide.EvaluatePosition(fallbackT);
            }

            // Start point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startPos, 0.3f);

            // End point color
            Gizmos.color = canReach ? Color.red : Color.blue;
            Gizmos.DrawSphere(endPos, 0.3f);

            // Arc color
            Gizmos.color = canReach ? Color.yellow : Color.magenta;

            Vector3 prev = startPos;

            for (int i = 1; i <= arcResolution; i++)
            {
                float t = i / (float)arcResolution;

                Vector3 linear = Vector3.Lerp(startPos, endPos, t);
                float arc = 4f * jumpHeight * t * (1f - t);

                Vector3 point = linear + Vector3.up * arc;

                Gizmos.DrawLine(prev, point);
                prev = point;
            }

            // Max jump range
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
            Gizmos.DrawWireSphere(startPos, maxJumpDistance);
        }
    }
}