using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MovementTemplate
{
    /// <summary>
    /// Samples keyboard, mouse and gamepad into an <see cref="InputFrame"/> for the motor.
    /// Actions are created in code, so no Input Actions asset is needed. A touch UI can feed the
    /// "External" fields and Inject methods without any change to the motor.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Look")]
        [Tooltip("Degrees per mouse pixel.")] public float mouseSensitivity = 0.09f;
        [Tooltip("Degrees per second at full stick.")] public float stickSensitivity = 160f;
        public bool invertY;

        [Header("Options (hold is the default)")]
        public bool sprintToggle;
        public bool crouchToggle;
        public bool lockCursorOnStart = true;

        [Header("External input (touch UI writes here)")]
        public Vector2 externalMove;
        public Vector2 externalLookDelta;
        public bool externalSprint;
        public bool externalCrouch;

        public event Action SwitchPerspectivePressed;
        public event Action ShoulderSwapPressed;

        InputAction moveAction, lookMouseAction, lookStickAction, jumpAction, sprintAction,
                    crouchAction, interactAction, switchAction, shoulderAction;

        bool jumpLatch, crouchLatch, interactLatch;
        bool sprintToggled, crouchToggled;

        void Awake()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick", processors: "stickDeadzone");

            lookMouseAction = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            lookStickAction = new InputAction("LookStick", InputActionType.Value);
            lookStickAction.AddBinding("<Gamepad>/rightStick", processors: "stickDeadzone");

            jumpAction = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            sprintAction = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            crouchAction = Button("Crouch", "<Keyboard>/c", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            interactAction = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            switchAction = Button("SwitchPerspective", "<Keyboard>/v", "<Gamepad>/dpad/up");
            shoulderAction = Button("ShoulderSwap", "<Keyboard>/q", "<Gamepad>/dpad/left");

            jumpAction.performed += _ => jumpLatch = true;
            crouchAction.performed += _ => { crouchLatch = true; if (crouchToggle) crouchToggled = !crouchToggled; };
            sprintAction.performed += _ => { if (sprintToggle) sprintToggled = !sprintToggled; };
            interactAction.performed += _ => interactLatch = true;
            switchAction.performed += _ => SwitchPerspectivePressed?.Invoke();
            shoulderAction.performed += _ => ShoulderSwapPressed?.Invoke();
        }

        static InputAction Button(string name, params string[] paths)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var path in paths) action.AddBinding(path);
            return action;
        }

        void OnEnable()
        {
            moveAction.Enable(); lookMouseAction.Enable(); lookStickAction.Enable();
            jumpAction.Enable(); sprintAction.Enable(); crouchAction.Enable();
            interactAction.Enable(); switchAction.Enable(); shoulderAction.Enable();
            if (lockCursorOnStart) SetCursorLocked(true);
        }

        void OnDisable()
        {
            moveAction.Disable(); lookMouseAction.Disable(); lookStickAction.Disable();
            jumpAction.Disable(); sprintAction.Disable(); crouchAction.Disable();
            interactAction.Disable(); switchAction.Disable(); shoulderAction.Disable();
        }

        void OnDestroy()
        {
            moveAction?.Dispose(); lookMouseAction?.Dispose(); lookStickAction?.Dispose();
            jumpAction?.Dispose(); sprintAction?.Dispose(); crouchAction?.Dispose();
            interactAction?.Dispose(); switchAction?.Dispose(); shoulderAction?.Dispose();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                SetCursorLocked(true);
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public Vector2 MoveValue => Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>() + externalMove, 1f);

        /// <summary>Look change in degrees (x = yaw, y = pitch, up is positive) for this frame.</summary>
        public Vector2 ReadLook(float dt)
        {
            Vector2 look = lookMouseAction.ReadValue<Vector2>() * mouseSensitivity;
            look += lookStickAction.ReadValue<Vector2>() * (stickSensitivity * dt);
            look += externalLookDelta;
            externalLookDelta = Vector2.zero;
            if (invertY) look.y = -look.y;
            return look;
        }

        public void InjectJump() { jumpLatch = true; }
        public void InjectInteract() { interactLatch = true; }
        public void InjectSwitchPerspective() { SwitchPerspectivePressed?.Invoke(); }

        /// <summary>Called by the motor once per simulation tick.</summary>
        public InputFrame Consume(int tick)
        {
            Vector2 move = MoveValue;
            if (move.sqrMagnitude < 0.0001f) sprintToggled = false;

            var frame = new InputFrame
            {
                Tick = tick,
                Move = move,
                JumpPressed = jumpLatch,
                JumpHeld = jumpAction.IsPressed(),
                SprintHeld = sprintToggle ? sprintToggled : (sprintAction.IsPressed() || externalSprint),
                CrouchHeld = crouchToggle ? crouchToggled : (crouchAction.IsPressed() || externalCrouch),
                CrouchPressed = crouchLatch,
                InteractPressed = interactLatch
            };
            jumpLatch = false;
            crouchLatch = false;
            interactLatch = false;
            return frame;
        }
    }
}
