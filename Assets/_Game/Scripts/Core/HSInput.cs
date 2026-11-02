using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.IsartDigital.Hyperspeed
{
    // Device-level input facade (keyboard + mouse + gamepad), no action asset needed.
    public static class HSInput
    {
        private const float STICK_DEADZONE = 0.2f;
        private const float MENU_THRESHOLD = 0.5f;

        private static int _lastMenuFrame = -1;
        private static Vector2Int _menuDir;
        private static Vector2Int _lastMenuDir;
        private static float _menuRepeatTimer;

        // Virtual inputs written by the autopilot (attract / test mode).
        public static float BotSteer;
        public static bool BotSlideHeld;
        public static bool BotGlideHeld;
        public static int BotJumpFrame = -1;
        public static int BotDashFrame = -1;
        public static int BotSlideFrame = -1;
        public static int BotPunchFrame = -1;

        private static bool Bot(int pFrame) => pFrame == Time.frameCount;

        private static Keyboard Kb => Keyboard.current;
        private static Gamepad Pad => Gamepad.current;
        private static Mouse Ms => Mouse.current;

        public static float Steer
        {
            get
            {
                float lValue = 0f;
                if (Kb != null)
                {
                    if (Kb.aKey.isPressed || Kb.qKey.isPressed || Kb.leftArrowKey.isPressed) lValue -= 1f;
                    if (Kb.dKey.isPressed || Kb.rightArrowKey.isPressed) lValue += 1f;
                }
                if (Pad != null)
                {
                    float lStick = Pad.leftStick.ReadValue().x;
                    if (Mathf.Abs(lStick) > STICK_DEADZONE) lValue += lStick;
                    if (Pad.dpad.left.isPressed) lValue -= 1f;
                    if (Pad.dpad.right.isPressed) lValue += 1f;
                }
                return Mathf.Clamp(lValue + BotSteer, -1f, 1f);
            }
        }

        public static float Forward
        {
            get
            {
                float lValue = 0f;
                if (Kb != null)
                {
                    if (Kb.wKey.isPressed || Kb.zKey.isPressed || Kb.upArrowKey.isPressed) lValue += 1f;
                    if (Kb.sKey.isPressed || Kb.downArrowKey.isPressed) lValue -= 1f;
                }
                if (Pad != null)
                {
                    float lStick = Pad.leftStick.ReadValue().y;
                    if (Mathf.Abs(lStick) > STICK_DEADZONE) lValue += lStick;
                }
                return Mathf.Clamp(lValue, -1f, 1f);
            }
        }

        public static bool JumpPressed => Bot(BotJumpFrame) ||
            (Kb != null && Kb.spaceKey.wasPressedThisFrame) || (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        public static bool ForwardPressed =>
            (Kb != null && (Kb.wKey.wasPressedThisFrame || Kb.zKey.wasPressedThisFrame || Kb.upArrowKey.wasPressedThisFrame)) ||
            (Pad != null && (Pad.rightTrigger.wasPressedThisFrame || Pad.dpad.up.wasPressedThisFrame));

        public static bool JumpHeld => BotGlideHeld ||
            (Kb != null && Kb.spaceKey.isPressed) || (Pad != null && Pad.buttonSouth.isPressed);

        public static bool DashHeld =>
            (Kb != null && Kb.leftShiftKey.isPressed) || (Pad != null && (Pad.rightShoulder.isPressed || Pad.buttonEast.isPressed));

        // Camera orbit while free roaming : mouse (cursor locked) or right stick, in degrees this frame.
        public static Vector2 LookDelta
        {
            get
            {
                Vector2 lValue = Vector2.zero;
                if (Ms != null) lValue += Ms.delta.ReadValue() * 0.12f;
                if (Pad != null)
                {
                    Vector2 lStick = Pad.rightStick.ReadValue();
                    if (lStick.magnitude > STICK_DEADZONE) lValue += lStick * 160f * Time.unscaledDeltaTime;
                }
                return lValue;
            }
        }

        public static bool DashPressed => Bot(BotDashFrame) ||
            (Kb != null && Kb.leftShiftKey.wasPressedThisFrame) || (Ms != null && Ms.rightButton.wasPressedThisFrame) ||
            (Pad != null && (Pad.rightShoulder.wasPressedThisFrame || Pad.buttonEast.wasPressedThisFrame));

        // Crouch. Ground : slide / drift, drop through cracked floors. Air : dive to the platform below.
        public static bool SlidePressed => Bot(BotSlideFrame) ||
            (Kb != null && (Kb.leftCtrlKey.wasPressedThisFrame || Kb.cKey.wasPressedThisFrame || Kb.sKey.wasPressedThisFrame || Kb.downArrowKey.wasPressedThisFrame)) ||
            (Pad != null && (Pad.leftTrigger.wasPressedThisFrame || Pad.leftShoulder.wasPressedThisFrame || Pad.dpad.down.wasPressedThisFrame));

        public static bool SlideHeld => BotSlideHeld ||
            (Kb != null && (Kb.leftCtrlKey.isPressed || Kb.cKey.isPressed || Kb.sKey.isPressed || Kb.downArrowKey.isPressed)) ||
            (Pad != null && (Pad.leftTrigger.isPressed || Pad.leftShoulder.isPressed || Pad.dpad.down.isPressed));

        public static bool PunchPressed => Bot(BotPunchFrame) ||
            (Kb != null && (Kb.eKey.wasPressedThisFrame || Kb.fKey.wasPressedThisFrame)) || (Ms != null && Ms.leftButton.wasPressedThisFrame) ||
            (Pad != null && (Pad.buttonWest.wasPressedThisFrame || Pad.rightTrigger.wasPressedThisFrame));

        public static bool RestartPressed =>
            (Kb != null && Kb.rKey.wasPressedThisFrame) || (Pad != null && Pad.selectButton.wasPressedThisFrame);

        public static bool AutopilotTogglePressed => Kb != null && Kb.f9Key.wasPressedThisFrame;

        public static bool PausePressed =>
            (Kb != null && Kb.escapeKey.wasPressedThisFrame) || (Pad != null && Pad.startButton.wasPressedThisFrame);

        public static bool SubmitPressed =>
            (Kb != null && (Kb.enterKey.wasPressedThisFrame || Kb.spaceKey.wasPressedThisFrame || Kb.numpadEnterKey.wasPressedThisFrame)) ||
            (Pad != null && (Pad.buttonSouth.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame));

        public static bool BackPressed =>
            (Kb != null && (Kb.escapeKey.wasPressedThisFrame || Kb.backspaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonEast.wasPressedThisFrame);

        public static bool AnyPressed =>
            (Kb != null && Kb.anyKey.wasPressedThisFrame) || (Ms != null && Ms.leftButton.wasPressedThisFrame) ||
            (Pad != null && (Pad.buttonSouth.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame || Pad.buttonEast.wasPressedThisFrame));

        public static bool MouseClicked => Ms != null && Ms.leftButton.wasPressedThisFrame;

        public static bool MouseHeld => Ms != null && Ms.leftButton.isPressed;

        public static float MouseWheel => Ms != null ? Ms.scroll.ReadValue().y : 0f;

        // Confirm without the jump key (on the title the runner jumps with Space / A).
        public static bool SubmitMenuPressed =>
            (Kb != null && (Kb.enterKey.wasPressedThisFrame || Kb.numpadEnterKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.startButton.wasPressedThisFrame);

        public static Vector2 MousePosition => Ms != null ? Ms.position.ReadValue() : Vector2.zero;

        public static Vector2 MouseDelta => Ms != null ? Ms.delta.ReadValue() : Vector2.zero;

        // Menu navigation with key repeat, computed once per frame.
        public static Vector2Int MenuMove
        {
            get
            {
                if (_lastMenuFrame == Time.frameCount) return _menuDir;
                _lastMenuFrame = Time.frameCount;

                Vector2Int lRaw = Vector2Int.zero;
                if (Kb != null)
                {
                    if (Kb.upArrowKey.isPressed || Kb.wKey.isPressed || Kb.zKey.isPressed) lRaw.y += 1;
                    if (Kb.downArrowKey.isPressed || Kb.sKey.isPressed) lRaw.y -= 1;
                    if (Kb.leftArrowKey.isPressed || Kb.aKey.isPressed || Kb.qKey.isPressed) lRaw.x -= 1;
                    if (Kb.rightArrowKey.isPressed || Kb.dKey.isPressed) lRaw.x += 1;
                }
                if (Pad != null)
                {
                    Vector2 lStick = Pad.leftStick.ReadValue() + Pad.dpad.ReadValue();
                    if (lStick.y > MENU_THRESHOLD) lRaw.y += 1;
                    if (lStick.y < -MENU_THRESHOLD) lRaw.y -= 1;
                    if (lStick.x > MENU_THRESHOLD) lRaw.x += 1;
                    if (lStick.x < -MENU_THRESHOLD) lRaw.x -= 1;
                }
                lRaw.x = Mathf.Clamp(lRaw.x, -1, 1);
                lRaw.y = Mathf.Clamp(lRaw.y, -1, 1);

                _menuDir = Vector2Int.zero;
                if (lRaw != _lastMenuDir)
                {
                    _menuDir = lRaw;
                    _menuRepeatTimer = 0.35f;
                }
                else if (lRaw != Vector2Int.zero)
                {
                    _menuRepeatTimer -= Time.unscaledDeltaTime;
                    if (_menuRepeatTimer <= 0f)
                    {
                        _menuDir = lRaw;
                        _menuRepeatTimer = 0.1f;
                    }
                }
                _lastMenuDir = lRaw;
                return _menuDir;
            }
        }
    }
}
