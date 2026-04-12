using UnityEngine;
using WaterSlide.Data;
using WaterSlide.Player;

namespace WaterSlide.Spline
{
    public class SplineFollowMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform followTarget;

        public void TickFollow(PlayerSlideRuntime runtime, PlayerSlideData data, float deltaTime)
        {
            if (runtime == null || data == null)
                return;

            if (runtime.CurrentSlide == null)
                return;

            if (followTarget == null)
                followTarget = transform;

            float splineLength = runtime.CurrentSlide.GetLength();
            if (splineLength <= 0.001f)
                return;

            float deltaT = (runtime.CurrentSpeed / splineLength) * deltaTime;
            runtime.CurrentT += deltaT;

            if (runtime.CurrentT >= 1f)
            {
                runtime.CurrentT = 1f;
            }

            Pose targetPose = runtime.CurrentSlide.EvaluatePose(runtime.CurrentT, data.useSplineUp);

            if (data.positionLerpSpeed > 0f)
            {
                followTarget.position = Vector3.Lerp(
                    followTarget.position,
                    targetPose.position,
                    data.positionLerpSpeed * deltaTime
                );
            }
            else
            {
                followTarget.position = targetPose.position;
            }

            if (data.enableRotationSmoothing && data.rotationLerpSpeed > 0f)
            {
                followTarget.rotation = Quaternion.Slerp(
                    followTarget.rotation,
                    targetPose.rotation,
                    data.rotationLerpSpeed * deltaTime
                );
            }
            else
            {
                followTarget.rotation = targetPose.rotation;
            }
        }

        public void SnapToSpline(PlayerSlideRuntime runtime, PlayerSlideData data)
        {
            if (runtime == null || data == null || runtime.CurrentSlide == null)
                return;

            if (followTarget == null)
                followTarget = transform;

            Pose pose = runtime.CurrentSlide.EvaluatePose(runtime.CurrentT, data.useSplineUp);
            followTarget.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}