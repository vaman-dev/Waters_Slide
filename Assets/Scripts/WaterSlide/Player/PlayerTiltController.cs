using UnityEngine;

namespace WaterSlide.Player
{
    public class PlayerTiltController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform tiltTarget;

        [Header("Tilt Settings")]
        [SerializeField] private float maxTiltAngle = 18f;
        [SerializeField] private float tiltLerpSpeed = 8f;
        [SerializeField] private bool invertTilt = false;

        private Quaternion initialLocalRotation;

        private void Awake()
        {
            if (inputReader == null)
                inputReader = GetComponent<PlayerInputReader>();

            if (tiltTarget == null)
            {
                Debug.LogError("[PlayerTiltController] Tilt Target is missing.", this);
                enabled = false;
                return;
            }

            initialLocalRotation = tiltTarget.localRotation;
        }

        private void LateUpdate()
        {
            if (inputReader == null || tiltTarget == null)
                return;

            float horizontal = inputReader.HorizontalInput;

            if (invertTilt)
                horizontal *= -1f;

            float targetZ = -horizontal * maxTiltAngle;

            Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(0f, 0f, targetZ);

            tiltTarget.localRotation = Quaternion.Slerp(
                tiltTarget.localRotation,
                targetRotation,
                tiltLerpSpeed * Time.deltaTime
            );
        }
    }
}