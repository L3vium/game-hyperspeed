using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum CursorMode { Free, Menu, Game }

    // One owner for the mouse cursor. In a race it is captured (locked, hidden) and can never leave the window ; in menus
    // it is visible but kept inside the window when the game is fullscreen. Losing the focus releases it ; getting the
    // focus back captures it again and swallows the click that brought the window back, so it never reaches the game.
    public static class CursorGuard
    {
        public const float REFOCUS_BLOCK = 0.3f;

        public static CursorMode Mode { get; private set; } = CursorMode.Free;

        public static void Set(CursorMode pMode)
        {
            Mode = pMode;
            Apply();
        }

        public static void Apply()
        {
            if (!Application.isFocused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            switch (Mode)
            {
                case CursorMode.Game:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case CursorMode.Menu:
                    Cursor.lockState = Screen.fullScreenMode == FullScreenMode.Windowed ? CursorLockMode.None : CursorLockMode.Confined;
                    Cursor.visible = true;
                    break;
                default:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Mode = CursorMode.Free;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            GameObject lObject = new GameObject("CursorGuard");
            Object.DontDestroyOnLoad(lObject);
            lObject.hideFlags = HideFlags.HideInHierarchy;
            lObject.AddComponent<CursorGuardBehaviour>();
        }
    }

    public class CursorGuardBehaviour : MonoBehaviour
    {
        // Fired when the window loses the focus : the race pauses itself.
        public static event System.Action OnFocusLost;

        private void OnApplicationFocus(bool pFocus)
        {
            if (pFocus) HSInput.BlockedUntil = Time.unscaledTime + CursorGuard.REFOCUS_BLOCK;
            else OnFocusLost?.Invoke();
            CursorGuard.Apply();
        }

        private void Update()
        {
            if (!Application.isFocused) return;
            // Something released the capture while racing (Alt-Tab, Esc in the editor, a dialog) : take it back.
            if (CursorGuard.Mode == CursorMode.Game && Cursor.lockState != CursorLockMode.Locked) CursorGuard.Apply();
            else if (CursorGuard.Mode == CursorMode.Menu && Screen.fullScreenMode != FullScreenMode.Windowed && Cursor.lockState != CursorLockMode.Confined) CursorGuard.Apply();
        }
    }
}
