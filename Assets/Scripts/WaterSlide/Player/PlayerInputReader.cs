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

        private InputAction moveAction;
        private Vector2 moveInput;

        public Vector2 MoveInput => moveInput;
        public float HorizontalInput => moveInput.x;
        public float VerticalInput => moveInput.y;

        private void Awake()
        {
            BindActions();
        }

        private void OnEnable()
        {
            if (moveAction != null)
                moveAction.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null)
                moveAction.Disable();
        }

        private void Update()
        {
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();
            else
                moveInput = Vector2.zero;
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
        }
    }
}