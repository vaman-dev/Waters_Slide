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

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction lookAction;

        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpPressed;

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
        }

        private void OnDisable()
        {
            if (moveAction != null)
                moveAction.Disable();

            if (jumpAction != null)
                jumpAction.Disable();

            if (lookAction != null)
                lookAction.Disable();
        }

        private void OnDestroy()
        {
            if (jumpAction != null)
                jumpAction.performed -= OnJump;
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
            if (moveAction == null)
            {
                Debug.LogError($"[PlayerInputReader] Action '{moveActionName}' not found in map '{actionMapName}'.", this);
                return;
            }

            jumpAction = actionMap.FindAction(jumpActionName, true);
            if (jumpAction == null)
            {
                Debug.LogError($"[PlayerInputReader] Action '{jumpActionName}' not found in map '{actionMapName}'.", this);
                return;
            }

            lookAction = actionMap.FindAction(lookActionName, true);
            if (lookAction == null)
            {
                Debug.LogError($"[PlayerInputReader] Action '{lookActionName}' not found in map '{actionMapName}'.", this);
                return;
            }

            jumpAction.performed -= OnJump;
            jumpAction.performed += OnJump;
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                jumpPressed = true;
        }
    }
}