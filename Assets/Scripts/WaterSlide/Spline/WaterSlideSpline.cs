using UnityEngine;
using UnityEngine.Splines;

namespace WaterSlide.Spline
{
    public class WaterSlideSpline : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SplineContainer splineContainer;

        [Header("Settings")]
        [SerializeField] private float followOffsetY = 0f;

        public SplineContainer SplineContainer => splineContainer;
        public float FollowOffsetY => followOffsetY;

        private void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
        }

        public float GetLength()
        {
            if (splineContainer == null || splineContainer.Spline == null)
                return 0f;

            return splineContainer.CalculateLength();
        }

        public Vector3 EvaluatePosition(float t)
        {
            if (splineContainer == null)
                return transform.position;

            Vector3 localPos = splineContainer.EvaluatePosition(t);
            return splineContainer.transform.TransformPoint(localPos);
        }

        public Vector3 EvaluateTangent(float t)
        {
            if (splineContainer == null)
                return transform.forward;

            Vector3 localTangent = splineContainer.EvaluateTangent(t);
            return splineContainer.transform.TransformDirection(localTangent).normalized;
        }

        public Vector3 EvaluateUp(float t)
        {
            if (splineContainer == null)
                return transform.up;

            Vector3 localUp = splineContainer.EvaluateUpVector(t);
            return splineContainer.transform.TransformDirection(localUp).normalized;
        }

        public Pose EvaluatePose(float t, bool useSplineUp)
        {
            Vector3 position = EvaluatePosition(t);
            Vector3 tangent = EvaluateTangent(t);
            Vector3 up = useSplineUp ? EvaluateUp(t) : Vector3.up;

            if (tangent.sqrMagnitude < 0.0001f)
                tangent = transform.forward;

            if (up.sqrMagnitude < 0.0001f)
                up = Vector3.up;

            Quaternion rotation = Quaternion.LookRotation(tangent, up);

            position += rotation * Vector3.up * followOffsetY;

            return new Pose(position, rotation);
        }
    }
}