using UnityEngine;
using UnityEngine.InputSystem;

namespace WaterSlide.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Source")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Action Names")]
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string abilityActionName = "Ability";

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction lookAction;
        private InputAction abilityAction;

        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpPressed;
        private bool abilityPressed;

        public Vector2 MoveInput => moveInput;
        public Vector2 LookInput => lookInput;
        public float HorizontalInput => moveInput.x;
        public float VerticalInput => moveInput.y;

        public bool ConsumeJumpPressed()
        {
            bool wasPressed = jumpPressed;
            jumpPressed = false;
            return wasPressed;
        }

        public bool ConsumeAbilityPressed()
        {
            bool wasPressed = abilityPressed;
            abilityPressed = false;
            return wasPressed;
        }

        private void Awake()
        {
            BindActions();
        }

        private void OnEnable()
        {
            if (moveAction != null)
                moveAction.Enable();

            if (jumpAction != null)
                jumpAction.Enable();

            if (lookAction != null)
                lookAction.Enable();

            if (abilityAction != null)
                abilityAction.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null)
                moveAction.Disable();

            if (jumpAction != null)
                jumpAction.Disable();

            if (lookAction != null)
                lookAction.Disable();

            if (abilityAction != null)
                abilityAction.Disable();
        }

        private void OnDestroy()
        {
            if (jumpAction != null)
                jumpAction.performed -= OnJumpPerformed;

            if (abilityAction != null)
                abilityAction.started += OnAbilityPerformed;
        }

        private void Update()
        {
            moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        }

        private void BindActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("[PlayerInputReader] InputActionAsset is missing.", this);
                return;
            }

            InputActionMap actionMap = inputActions.FindActionMap(actionMapName, true);
            if (actionMap == null)
            {
                Debug.LogError($"[PlayerInputReader] Action Map '{actionMapName}' not found.", this);
                return;
            }

            moveAction = actionMap.FindAction(moveActionName, true);
            jumpAction = actionMap.FindAction(jumpActionName, true);
            lookAction = actionMap.FindAction(lookActionName, true);
            abilityAction = actionMap.FindAction(abilityActionName, true);

            if (moveAction == null)
            {
                Debug.LogError($"[PlayerInputReader] Action '{moveActionName}' not found in map '{actionMapName}'.", this);
                return;
            }

            if (jumpAction == null)
            {
                Debug.LogError($"[PlayerInputReader] Action '{jumpActionName}' not found in map '{actionMapName}'.", this);
                return;
            }

            if (lookAction == null)
            {
                Debug.LogError($"[PlayerInputReader] Action '{lookActionName}' not found in map '{actionMapName}'.", this);
                return;
            }

            if (abilityAction == null)
            {
                Debug.LogError($"[PlayerInputReader] Action '{abilityActionName}' not found in map '{actionMapName}'.", this);
                return;
            }

            jumpAction.performed -= OnJumpPerformed;
            jumpAction.performed += OnJumpPerformed;

            abilityAction.performed -= OnAbilityPerformed;
            abilityAction.performed += OnAbilityPerformed;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
                jumpPressed = true;
        }

        private void OnAbilityPerformed(InputAction.CallbackContext context)
        {
            abilityPressed = true;
        }
    }
}