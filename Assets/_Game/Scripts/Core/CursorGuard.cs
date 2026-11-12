using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    public enum CursorMode { Free, Menu, Game }

    // One owner for the mouse cursor. In a race it is captured (locked, hidden) and can never leave the window ; in menus
    // (title, select, pause, results) it is visible and free to leave the window in every display mode (2026-11-12 :
    // "dans l'UI on doit pouvoir sortir la souris"). Losing the focus releases it ; getting the focus back captures it
    // again in a race and swallows the click that brought the window back, so it never reaches the game.
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
            else if (CursorGuard.Mode != CursorMode.Game && Cursor.lockState != CursorLockMode.None) CursorGuard.Apply();
        }
    }
}
