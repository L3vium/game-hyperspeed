using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.IsartDigital.Hyperspeed
{
    // Input facade. Gameplay buttons come from the rebindable actions (HSBindings) ; sticks and the mouse are read from
    // the devices with a rescaled dead zone and a response curve ; menu keys are fixed (A / Enter / Space confirm, B / Esc back).
    // You only run while you ask for it : W (or RT, or the left stick pushed anywhere but down).
    public static class HSInput
    {
        private const float STICK_DEADZONE = 0.18f;
        private const float MENU_THRESHOLD = 0.5f;
        // Right stick, degrees per second at full tilt before the sensitivity settings.
        private const float PAD_LOOK_X = 220f;
        private const float PAD_LOOK_Y = 165f;
        // Left stick sideways turns the runner (and the view with it), degrees per second at full tilt.
        public const float PAD_TURN = 175f;

        private static int _lastMenuFrame = -1;
        private static Vector2Int _menuDir;
        private static Vector2Int _lastMenuDir;
        private static float _menuRepeatTimer;

        private static int _lookFrame = -1;
        private static Vector2 _look;
        private static float _padFullTime;
        private static int _runFrame = -1;
        private static float _runPrevious;
        private static bool _runPressed;

        // Virtual inputs written by the autopilot (attract / test mode).
        public static float BotSteer;
        public static float BotRun;
        public static bool BotSlideHeld;
        public static bool BotDashHeld;
        public static bool BotAimActive;
        public static Vector3 BotAimDirection;
        public static float BotAimPitch;
        public static int BotJumpFrame = -1;
        public static int BotDashFrame = -1;
        public static int BotSlideFrame = -1;
        public static int BotPunchFrame = -1;
        private static float _botJumpTime = -10f;
        // Virtual mouse for the end-to-end menu tests.
        public static bool BotMouseActive;
        public static Vector2 BotMousePosition;
        public static bool BotMouseHeld;
        public static int BotMouseClickFrame = -1;

        private static bool Bot(int pFrame) => pFrame == Time.frameCount;

        private static Keyboard Kb => Keyboard.current;
        private static Gamepad Pad => Gamepad.current;
        private static Mouse Ms => Mouse.current;

        private static bool Pressed(string pAction) => !HSBindings.Rebinding && HSBindings.Get(pAction).WasPressedThisFrame();
        private static bool Held(string pAction) => !HSBindings.Rebinding && HSBindings.Get(pAction).IsPressed();

        // The bot jumps by holding the button for a moment, like a player : variable jumps keep their height.
        public static void BotJump()
        {
            BotJumpFrame = Time.frameCount + 1;
            _botJumpTime = Time.time;
        }

        // Dead zone rescaled to 0 at its edge, then a curve : fine aim near the centre, full speed at the rim.
        public static Vector2 Shape(Vector2 pStick, float pCurve)
        {
            float lMagnitude = pStick.magnitude;
            if (lMagnitude <= STICK_DEADZONE) return Vector2.zero;
            float lT = Mathf.Clamp01((lMagnitude - STICK_DEADZONE) / (1f - STICK_DEADZONE));
            return pStick / lMagnitude * Mathf.Pow(lT, pCurve);
        }

        public static Vector2 MoveStick => Pad != null ? Shape(Pad.leftStick.ReadValue(), 1.25f) : Vector2.zero;

        // Left stick pulled toward you : a brake, not a run.
        private static bool StickBraking(Vector2 pStick) => pStick.y < 0f && -pStick.y > Mathf.Abs(pStick.x);

        // Sidestep : keys and the d-pad only (the left stick turns).
        public static float Steer
        {
            get
            {
                float lValue = 0f;
                if (Held("StrafeLeft")) lValue -= 1f;
                if (Held("StrafeRight")) lValue += 1f;
                return Mathf.Clamp(lValue + BotSteer, -1f, 1f);
            }
        }

        // Gamepad left stick sideways : a turn of the runner and the view.
        public static float TurnStick => MoveStick.x;

        // How hard you ask to run, 0 to 1.
        public static float Run
        {
            get
            {
                float lValue = Held("Run") ? 1f : 0f;
                if (Pad != null && HSBindings.Get("Run").activeControl == Pad.rightTrigger) lValue = Mathf.Max(lValue, Pad.rightTrigger.ReadValue());
                Vector2 lStick = MoveStick;
                if (!StickBraking(lStick)) lValue = Mathf.Max(lValue, lStick.magnitude);
                return Mathf.Clamp01(Mathf.Max(lValue, BotRun));
            }
        }

        public static float Brake
        {
            get
            {
                float lValue = Held("Brake") ? 1f : 0f;
                Vector2 lStick = MoveStick;
                if (StickBraking(lStick)) lValue = Mathf.Max(lValue, -lStick.y);
                return lValue;
            }
        }

        // Free movement (start plaza) : forward / back relative to the view.
        public static float Forward => Mathf.Clamp((Held("Run") ? 1f : 0f) - (Held("Brake") ? 1f : 0f) + (Pad != null ? Shape(Pad.leftStick.ReadValue(), 1f).y : 0f), -1f, 1f);

        // Free movement (start plaza) : sideways relative to the view.
        public static float Sideways => Mathf.Clamp(Steer + (Pad != null ? Shape(Pad.leftStick.ReadValue(), 1f).x : 0f), -1f, 1f);

        // A fresh "go" this frame : the run key, the trigger, or the stick pushed past half way.
        public static bool RunPressed
        {
            get
            {
                if (_runFrame != Time.frameCount)
                {
                    _runFrame = Time.frameCount;
                    float lNow = Run;
                    _runPressed = Pressed("Run") || (lNow > 0.5f && _runPrevious <= 0.5f);
                    _runPrevious = lNow;
                }
                return _runPressed;
            }
        }

        public static bool JumpPressed => Bot(BotJumpFrame) || Pressed("Jump");

        public static bool JumpHeld => Held("Jump") || Time.time - _botJumpTime < 0.45f;

        public static bool DashHeld => BotDashHeld || Held("Dash");

        // Where you look : mouse (cursor locked) or right stick, in degrees this frame. Computed once per frame.
        public static Vector2 LookDelta
        {
            get
            {
                if (_lookFrame == Time.frameCount) return _look;
                _lookFrame = Time.frameCount;
                Vector2 lValue = Vector2.zero;
                if (Ms != null) lValue += Ms.delta.ReadValue() * 0.07f * GameSession.MouseSensitivity;
                if (Pad != null)
                {
                    Vector2 lStick = Shape(Pad.rightStick.ReadValue(), 1.6f);
                    float lDt = Time.unscaledDeltaTime;
                    // Held at the rim : the turn speeds up a little, for the hairpins.
                    _padFullTime = Mathf.Abs(lStick.x) > 0.92f ? _padFullTime + lDt : 0f;
                    float lRamp = 1f + Mathf.Clamp01((_padFullTime - 0.2f) / 0.35f) * 0.55f;
                    lValue.x += lStick.x * PAD_LOOK_X * GameSession.PadSensitivityX * lRamp * lDt;
                    lValue.y += lStick.y * PAD_LOOK_Y * GameSession.PadSensitivityY * lDt;
                }
                if (GameSession.InvertY) lValue.y = -lValue.y;
                _look = lValue;
                return lValue;
            }
        }

        public static bool DashPressed => Bot(BotDashFrame) || Pressed("Dash");

        // Ground only : slide, and drift with a turn. Diving is done by looking down.
        public static bool SlidePressed => Bot(BotSlideFrame) || Pressed("Drift");

        public static bool SlideHeld => BotSlideHeld || Held("Drift");

        public static bool PunchPressed => Bot(BotPunchFrame) || Pressed("Punch");

        public static bool RestartPressed => Pressed("Restart");

        public static bool PausePressed =>
            !HSBindings.Rebinding && ((Kb != null && Kb.escapeKey.wasPressedThisFrame) || (Pad != null && Pad.startButton.wasPressedThisFrame));

        // Menus : A / Enter / Space confirm. Start confirms too, except where it pauses.
        public static bool SubmitPressed =>
            !HSBindings.Rebinding && ((Kb != null && (Kb.enterKey.wasPressedThisFrame || Kb.spaceKey.wasPressedThisFrame || Kb.numpadEnterKey.wasPressedThisFrame)) ||
            (Pad != null && (Pad.buttonSouth.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame)));

        public static bool BackPressed =>
            !HSBindings.Rebinding && ((Kb != null && (Kb.escapeKey.wasPressedThisFrame || Kb.backspaceKey.wasPressedThisFrame)) ||
            (Pad != null && Pad.buttonEast.wasPressedThisFrame));

        public static bool AnyPressed =>
            (Kb != null && Kb.anyKey.wasPressedThisFrame) || (Ms != null && Ms.leftButton.wasPressedThisFrame) ||
            (Pad != null && (Pad.buttonSouth.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame || Pad.buttonEast.wasPressedThisFrame));

        public static bool MouseClicked => BotMouseActive ? Bot(BotMouseClickFrame) : Ms != null && Ms.leftButton.wasPressedThisFrame;

        public static bool MouseHeld => BotMouseActive ? BotMouseHeld : Ms != null && Ms.leftButton.isPressed;

        public static float MouseWheel => Ms != null ? Ms.scroll.ReadValue().y : 0f;

        // Confirm in menus where Space has another use : Enter, A or Start.
        public static bool SubmitMenuPressed =>
            !HSBindings.Rebinding && ((Kb != null && (Kb.enterKey.wasPressedThisFrame || Kb.numpadEnterKey.wasPressedThisFrame)) ||
            (Pad != null && (Pad.buttonSouth.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame)));

        public static Vector2 MousePosition => BotMouseActive ? BotMousePosition : Ms != null ? Ms.position.ReadValue() : Vector2.zero;

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
                    if (Kb.upArrowKey.isPressed || Kb.wKey.isPressed) lRaw.y += 1;
                    if (Kb.downArrowKey.isPressed || Kb.sKey.isPressed) lRaw.y -= 1;
                    if (Kb.leftArrowKey.isPressed || Kb.aKey.isPressed) lRaw.x -= 1;
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
                if (HSBindings.Rebinding) lRaw = Vector2Int.zero;

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
