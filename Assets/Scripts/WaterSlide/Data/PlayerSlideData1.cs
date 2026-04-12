using UnityEngine;

namespace WaterSlide.Data
{
    [CreateAssetMenu(fileName = "PlayerSlideData", menuName = "WaterSlide/Player Slide Data")]
    public class PlayerSlideData : ScriptableObject
    {
        [Header("Speed")]
        [Min(0f)] public float startSpeed = 8f;
        [Min(0f)] public float maxSpeed = 18f;
        [Min(0f)] public float acceleration = 4f;
        [Min(0f)] public float deceleration = 1f;

        [Header("Spline Start")]
        [Range(0f, 1f)] public float startT = 0f;

        [Header("Smoothing")]
        [Min(0f)] public float positionLerpSpeed = 12f;
        [Min(0f)] public float rotationLerpSpeed = 8f;

        [Header("Camera / Orientation")]
        public bool useSplineUp = true;
        public bool enableRotationSmoothing = true;

        [Header("Finish")]
        public bool stopAtSplineEnd = true;
    }
}