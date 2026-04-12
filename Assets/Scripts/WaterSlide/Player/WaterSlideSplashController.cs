using UnityEngine;
using WaterSlide.Player;

namespace WaterSlide.Visuals
{
    public class WaterSlideSplashController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSlideController playerSlideController;

        [Header("Splash Particle Systems")]
        [SerializeField] private ParticleSystem frontSplash;
        [SerializeField] private ParticleSystem leftSplash;
        [SerializeField] private ParticleSystem rightSplash;
        [SerializeField] private ParticleSystem bodySplash;

        [Header("Speed Settings")]
        [SerializeField] private float splashStartSpeed = 0.5f;
        [SerializeField] private float maxSpeed = 18f;

        [Header("Emission Rates")]
        [SerializeField] private float frontMinRate = 5f;
        [SerializeField] private float frontMaxRate = 45f;
        [SerializeField] private float sideMinRate = 3f;
        [SerializeField] private float sideMaxRate = 35f;
        [SerializeField] private float bodyMinRate = 2f;
        [SerializeField] private float bodyMaxRate = 25f;

        [Header("Slide Contact")]
        [SerializeField] private bool isOnWaterSlide = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private ParticleSystem.EmissionModule frontEmission;
        private ParticleSystem.EmissionModule leftEmission;
        private ParticleSystem.EmissionModule rightEmission;
        private ParticleSystem.EmissionModule bodyEmission;

        private void Awake()
        {
            if (playerSlideController == null)
                playerSlideController = GetComponent<PlayerSlideController>();

            CacheEmissionModules();
            ValidateReferences();
            ForcePlayAll();
        }

        private void OnEnable()
        {
            ForcePlayAll();
        }

        private void Update()
        {
            if (playerSlideController == null)
            {
                SetEmissionRates(0f, 0f, 0f, 0f);
                return;
            }

            float speed = playerSlideController.Runtime.CurrentSpeed;

            if (debugLogs)
                Debug.Log($"[Splash] Speed: {speed:F2}, OnSlide: {isOnWaterSlide}", this);

            HandleContinuousSplash(speed);
        }

        private void CacheEmissionModules()
        {
            if (frontSplash != null) frontEmission = frontSplash.emission;
            if (leftSplash != null) leftEmission = leftSplash.emission;
            if (rightSplash != null) rightEmission = rightSplash.emission;
            if (bodySplash != null) bodyEmission = bodySplash.emission;
        }

        private void ValidateReferences()
        {
            if (playerSlideController == null)
                Debug.LogError("[WaterSlideSplashController] PlayerSlideController reference is missing.", this);

            if (frontSplash == null)
                Debug.LogWarning("[WaterSlideSplashController] FrontSplash is missing.", this);

            if (leftSplash == null)
                Debug.LogWarning("[WaterSlideSplashController] LeftSplash is missing.", this);

            if (rightSplash == null)
                Debug.LogWarning("[WaterSlideSplashController] RightSplash is missing.", this);

            if (bodySplash == null)
                Debug.LogWarning("[WaterSlideSplashController] BodySplash is missing.", this);
        }

        private void HandleContinuousSplash(float speed)
        {
            if (!isOnWaterSlide || speed < splashStartSpeed)
            {
                SetEmissionRates(0f, 0f, 0f, 0f);
                return;
            }

            float speed01 = Mathf.InverseLerp(splashStartSpeed, maxSpeed, speed);

            float frontRate = Mathf.Lerp(frontMinRate, frontMaxRate, speed01);
            float sideRate = Mathf.Lerp(sideMinRate, sideMaxRate, speed01);
            float bodyRate = Mathf.Lerp(bodyMinRate, bodyMaxRate, speed01);

            SetEmissionRates(frontRate, sideRate, sideRate, bodyRate);
        }

        private void SetEmissionRates(float frontRate, float leftRate, float rightRate, float bodyRate)
        {
            if (frontSplash != null)
            {
                frontEmission.enabled = true;
                frontEmission.rateOverTime = frontRate;
            }

            if (leftSplash != null)
            {
                leftEmission.enabled = true;
                leftEmission.rateOverTime = leftRate;
            }

            if (rightSplash != null)
            {
                rightEmission.enabled = true;
                rightEmission.rateOverTime = rightRate;
            }

            if (bodySplash != null)
            {
                bodyEmission.enabled = true;
                bodyEmission.rateOverTime = bodyRate;
            }
        }

        private void ForcePlayAll()
        {
            PlayIfValid(frontSplash);
            PlayIfValid(leftSplash);
            PlayIfValid(rightSplash);
            PlayIfValid(bodySplash);
        }

        private void PlayIfValid(ParticleSystem ps)
        {
            if (ps == null)
                return;

            if (!ps.isPlaying)
                ps.Play(true);
        }

        public void PlayLandingSplash(int frontBurst = 20, int sideBurst = 12, int bodyBurst = 16)
        {
            EmitBurst(frontSplash, frontBurst);
            EmitBurst(leftSplash, sideBurst);
            EmitBurst(rightSplash, sideBurst);
            EmitBurst(bodySplash, bodyBurst);
        }

        private void EmitBurst(ParticleSystem ps, int count)
        {
            if (ps == null || count <= 0)
                return;

            ps.Emit(count);
        }

        public void SetSlideContact(bool value)
        {
            isOnWaterSlide = value;

            if (!isOnWaterSlide)
                SetEmissionRates(0f, 0f, 0f, 0f);
        }
    }
}