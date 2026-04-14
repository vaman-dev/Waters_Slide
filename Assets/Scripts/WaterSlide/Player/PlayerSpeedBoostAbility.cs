using System;
using UnityEngine;

namespace WaterSlide.Player
{
    public class PlayerSpeedBoostAbility : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSlideController playerSlideController;
        [SerializeField] private PlayerInputReader inputReader;

        [Header("Boost Settings")]
        [SerializeField] private float boostAmount = 8f;
        [SerializeField] private float boostDuration = 1.5f;
        [SerializeField] private float cooldownDuration = 5f;
        [SerializeField] private float maxBoostSpeedClamp = 40f;

        [Header("Runtime")]
        [SerializeField] private bool isBoostActive;
        [SerializeField] private bool isOnCooldown;
        [SerializeField] private float boostTimer;
        [SerializeField] private float cooldownTimer;

        public bool IsBoostActive => isBoostActive;
        public bool IsOnCooldown => isOnCooldown;
        public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);
        public float CooldownNormalized =>
            cooldownDuration > 0f ? Mathf.Clamp01(cooldownTimer / cooldownDuration) : 0f;

        public event Action OnBoostUsed;
        public event Action<float> OnCooldownStarted;
        public event Action<float, float> OnCooldownUpdated;
        public event Action OnCooldownFinished;
        public event Action<bool> OnBoostStateChanged;

        private void Reset()
        {
            playerSlideController = GetComponent<PlayerSlideController>();
            inputReader = GetComponent<PlayerInputReader>();
        }

        private void Awake()
        {
            if (playerSlideController == null)
                playerSlideController = GetComponent<PlayerSlideController>();

            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            HandleAbilityInput();
            TickBoost(Time.deltaTime);
            TickCooldown(Time.deltaTime);
        }

        private void HandleAbilityInput()
        {
            if (inputReader == null)
                return;

            if (inputReader.ConsumeAbilityPressed())
            {
                TryActivateBoost();
            }
        }

        public bool TryActivateBoost()
        {
            if (playerSlideController == null)
                return false;

            if (isBoostActive || isOnCooldown)
                return false;

            if (playerSlideController.Runtime == null)
                return false;

            if (playerSlideController.Runtime.CurrentState == PlayerSlideState.Paused ||
                playerSlideController.Runtime.CurrentState == PlayerSlideState.Finished)
                return false;

            ActivateBoost();
            return true;
        }

        private void ActivateBoost()
        {
            float currentSpeed = playerSlideController.Runtime.CurrentSpeed;

            float boostedSpeed = Mathf.Min(
                currentSpeed + boostAmount,
                maxBoostSpeedClamp
            );

            playerSlideController.SetSpeed(boostedSpeed);

            isBoostActive = true;
            boostTimer = boostDuration;

            Debug.Log($"[BOOST] Activated | Speed: {currentSpeed:F2} → {boostedSpeed:F2}");

            OnBoostUsed?.Invoke();
            OnBoostStateChanged?.Invoke(true);
        }

        private void TickBoost(float deltaTime)
        {
            if (!isBoostActive)
                return;

            boostTimer -= deltaTime;

            if (boostTimer <= 0f)
            {
                EndBoost();
            }
        }

        private void EndBoost()
        {
            isBoostActive = false;
            boostTimer = 0f;

            OnBoostStateChanged?.Invoke(false);
            StartCooldown();
        }

        private void StartCooldown()
        {
            isOnCooldown = true;
            cooldownTimer = cooldownDuration;

            OnCooldownStarted?.Invoke(cooldownDuration);
            OnCooldownUpdated?.Invoke(cooldownTimer, CooldownNormalized);
        }

        private void TickCooldown(float deltaTime)
        {
            if (!isOnCooldown)
                return;

            cooldownTimer -= deltaTime;
            cooldownTimer = Mathf.Max(0f, cooldownTimer);

            OnCooldownUpdated?.Invoke(cooldownTimer, CooldownNormalized);

            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                OnCooldownFinished?.Invoke();
            }
        }
    }
}