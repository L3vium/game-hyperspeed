using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.IsartDigital.Hyperspeed
{
    // Rebindable gameplay buttons, built in code. Each action has a keyboard / mouse binding group and a gamepad one;
    // overrides are saved as JSON in the PlayerPrefs. Sticks and the mouse look stay read from the devices.
    public static class HSBindings
    {
        private const string PREFS_KEY = "hs_bindings";

        public struct Entry
        {
            public string action;
            public string label;
        }

        // What the Controls screen lists, in order.
        public static readonly Entry[] REBINDABLE =
        {
            new Entry { action = "Run", label = "RUN" },
            new Entry { action = "Brake", label = "BRAKE" },
            new Entry { action = "StrafeLeft", label = "SIDESTEP LEFT" },
            new Entry { action = "StrafeRight", label = "SIDESTEP RIGHT" },
            new Entry { action = "Jump", label = "JUMP" },
            new Entry { action = "Dash", label = "DASH" },
            new Entry { action = "Drift", label = "DRIFT / SLIDE" },
            new Entry { action = "Punch", label = "PUNCH" },
            new Entry { action = "Restart", label = "RESTART" },
        };

        private static InputActionMap _map;
        private static InputActionRebindingExtensions.RebindingOperation _rebind;

        public static bool Rebinding => _rebind != null;

        public static InputActionMap Map
        {
            get
            {
                if (_map == null) Build();
                return _map;
            }
        }

        public static InputAction Get(string pName) => Map.FindAction(pName, true);

        private static void Build()
        {
            _map = new InputActionMap("Gameplay");
            Add("Run", "<Keyboard>/w", "<Keyboard>/upArrow", "<Gamepad>/rightTrigger");
            Add("Brake", "<Keyboard>/s", "<Keyboard>/downArrow", "<Gamepad>/dpad/down");
            Add("StrafeLeft", "<Keyboard>/a", "<Keyboard>/leftArrow", "<Gamepad>/dpad/left");
            Add("StrafeRight", "<Keyboard>/d", "<Keyboard>/rightArrow", "<Gamepad>/dpad/right");
            Add("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Add("Dash", "<Keyboard>/leftShift", "<Mouse>/rightButton", "<Gamepad>/rightShoulder", "<Gamepad>/buttonEast");
            Add("Drift", "<Keyboard>/leftCtrl", "<Keyboard>/c", "<Gamepad>/leftTrigger", "<Gamepad>/leftShoulder");
            Add("Punch", "<Keyboard>/e", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
            Add("Restart", "<Keyboard>/r", "<Gamepad>/select");
            string lJson = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(lJson))
            {
                try { _map.LoadBindingOverridesFromJson(lJson); }
                catch (System.Exception lError) { Debug.LogWarning("[HS] bad binding overrides, reset : " + lError.Message); }
            }
            _map.Enable();
        }

        private static void Add(string pName, params string[] pPaths)
        {
            InputAction lAction = _map.AddAction(pName, InputActionType.Button);
            foreach (string lPath in pPaths) lAction.AddBinding(lPath);
        }

        private static bool IsPadPath(string pPath) => !string.IsNullOrEmpty(pPath) && pPath.StartsWith("<Gamepad>");

        // Bindings of one device group, in order.
        private static List<int> Indices(InputAction pAction, bool pGamepad)
        {
            List<int> lList = new List<int>();
            for (int i = 0; i < pAction.bindings.Count; i++)
                if (IsPadPath(pAction.bindings[i].path) == pGamepad) lList.Add(i);
            return lList;
        }

        public static string Display(string pAction, bool pGamepad)
        {
            InputAction lAction = Get(pAction);
            List<string> lNames = new List<string>();
            foreach (int i in Indices(lAction, pGamepad))
            {
                string lName = lAction.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if (!string.IsNullOrEmpty(lName) && !lNames.Contains(lName)) lNames.Add(lName.ToUpperInvariant());
            }
            return lNames.Count > 0 ? string.Join("  /  ", lNames) : "-";
        }

        // Waits for the next press on the device group and puts it on the first binding of that group.
        // Escape cancels. The other bindings of the group are cleared so one press = one key.
        public static void Rebind(string pAction, bool pGamepad, System.Action pDone)
        {
            CancelRebind();
            InputAction lAction = Get(pAction);
            List<int> lIndices = Indices(lAction, pGamepad);
            if (lIndices.Count == 0)
            {
                pDone?.Invoke();
                return;
            }
            _map.Disable();
            _rebind = lAction.PerformInteractiveRebinding(lIndices[0])
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Gamepad>/leftStick")
                .WithControlsExcluding("<Gamepad>/rightStick")
                .WithControlsExcluding("<Gamepad>/start")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithExpectedControlType("Button")
                .OnMatchWaitForAnother(0.08f)
                .OnComplete(pOperation =>
                {
                    for (int k = 1; k < lIndices.Count; k++) lAction.ApplyBindingOverride(lIndices[k], new InputBinding { overridePath = "" });
                    Finish(pDone);
                })
                .OnCancel(pOperation => Finish(pDone));
            // Keyboard rows take keyboard keys and mouse buttons only, the gamepad rows only the pad.
            if (pGamepad) _rebind.WithControlsExcluding("<Keyboard>").WithControlsExcluding("<Mouse>");
            else _rebind.WithControlsExcluding("<Gamepad>");
            _rebind.Start();
        }

        private static void Finish(System.Action pDone)
        {
            if (_rebind != null) _rebind.Dispose();
            _rebind = null;
            _map.Enable();
            Save();
            pDone?.Invoke();
        }

        public static void CancelRebind()
        {
            if (_rebind == null) return;
            _rebind.Cancel();
        }

        public static void ResetAll()
        {
            CancelRebind();
            Map.RemoveAllBindingOverrides();
            Save();
        }

        private static void Save()
        {
            PlayerPrefs.SetString(PREFS_KEY, Map.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }
    }
}
