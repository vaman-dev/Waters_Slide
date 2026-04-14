using UnityEngine;
using UnityEngine.UI;
using WaterSlide.Player;

namespace WaterSlide.UI
{
    public class BoostCooldownUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSpeedBoostAbility boostAbility;

        [Header("UI")]
        [SerializeField] private Image cooldownImage;

        [Header("Alpha Settings")]
        [SerializeField] private float activeAlpha = 1f;
        [SerializeField] private float cooldownAlpha = 0.5f; // 128 / 255 ≈ 0.5

        [Header("Smoothing")]
        [SerializeField] private float alphaLerpSpeed = 8f;

        private float targetAlpha;

        private void Reset()
        {
            cooldownImage = GetComponent<Image>();
        }

        private void Awake()
        {
            if (cooldownImage == null)
                cooldownImage = GetComponent<Image>();

            targetAlpha = activeAlpha;
            ApplyAlphaInstant(targetAlpha);
        }

        private void OnEnable()
        {
            if (boostAbility == null)
                return;

            boostAbility.OnCooldownUpdated += HandleCooldownUpdated;
            boostAbility.OnCooldownFinished += HandleCooldownFinished;
        }

        private void OnDisable()
        {
            if (boostAbility == null)
                return;

            boostAbility.OnCooldownUpdated -= HandleCooldownUpdated;
            boostAbility.OnCooldownFinished -= HandleCooldownFinished;
        }

        private void Update()
        {
            UpdateAlphaSmooth();
        }

        private void HandleCooldownUpdated(float remaining, float normalized)
        {
            // normalized = 1 → start of cooldown
            // normalized = 0 → end of cooldown

            // We want alpha to go from cooldownAlpha → activeAlpha smoothly
            float progress = 1f - normalized;

            targetAlpha = Mathf.Lerp(cooldownAlpha, activeAlpha, progress);
        }

        private void HandleCooldownFinished()
        {
            // Cooldown finished, set alpha back to active
            targetAlpha = activeAlpha;
        }

        private void UpdateAlphaSmooth()
        {
            if (cooldownImage == null)
                return;

            Color color = cooldownImage.color;

            float newAlpha = Mathf.Lerp(
                color.a,
                targetAlpha,
                alphaLerpSpeed * Time.deltaTime
            );

            color.a = newAlpha;

            cooldownImage.color = color;

            // 🔍 DEBUG
            Debug.Log($"UI Alpha: {newAlpha}");
        }

        private void ApplyAlphaInstant(float alpha)
        {
            if (cooldownImage == null)
                return;

            Color color = cooldownImage.color;
            color.a = alpha;
            cooldownImage.color = color;
        }
    }
}