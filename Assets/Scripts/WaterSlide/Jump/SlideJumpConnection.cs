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

        [Header("Gizmo")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private int arcResolution = 20;

        public WaterSlideSpline SourceSlide => sourceSlide;
        public WaterSlideSpline TargetSlide => targetSlide;
        public float TargetLandingT => targetLandingT;
        public float JumpDuration => jumpDuration;
        public float JumpHeight => jumpHeight;

        public bool IsValidForSlide(WaterSlideSpline currentSlide)
        {
            if (sourceSlide == null || targetSlide == null || currentSlide == null)
                return false;

            return currentSlide == sourceSlide;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || sourceSlide == null || targetSlide == null)
                return;

            // Approximate start (end of source slide)
            Vector3 startPos = sourceSlide.EvaluatePosition(0.95f);

            // Landing point
            Vector3 endPos = targetSlide.EvaluatePosition(targetLandingT);

            // Draw points
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startPos, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endPos, 0.3f);

            // Draw arc
            Gizmos.color = Color.yellow;

            Vector3 prevPoint = startPos;

            for (int i = 1; i <= arcResolution; i++)
            {
                float t = i / (float)arcResolution;

                Vector3 linear = Vector3.Lerp(startPos, endPos, t);
                float arc = 4f * jumpHeight * t * (1f - t);

                Vector3 point = linear + Vector3.up * arc;

                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}