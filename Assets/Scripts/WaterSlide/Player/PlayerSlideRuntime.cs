using WaterSlide.Spline;

namespace WaterSlide.Player
{
    [System.Serializable]
    public class PlayerSlideRuntime
    {
        public WaterSlideSpline CurrentSlide;
        public float CurrentT;
        public float CurrentSpeed;
        public PlayerSlideState CurrentState;

        public void ResetRuntime(WaterSlideSpline slide, float startT, float startSpeed)
        {
            CurrentSlide = slide;
            CurrentT = startT;
            CurrentSpeed = startSpeed;
            CurrentState = PlayerSlideState.FollowingSpline;
        }
    }
}