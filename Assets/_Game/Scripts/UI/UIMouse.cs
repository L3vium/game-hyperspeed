using UnityEngine;

namespace Com.IsartDigital.Hyperspeed
{
    // Where the mouse sits on the screen, from (-1, -1) to (1, 1), with a soft spring : every 3D title leans with it.
    // With a locked cursor (in a race) the motion of the mouse is accumulated instead.
    public static class UIMouse
    {
        private static Vector2 _value;
        private static Vector2 _locked;
        private static int _frame = -1;

        public static Vector2 Tilt
        {
            get
            {
                if (_frame == Time.frameCount) return _value;
                _frame = Time.frameCount;
                Vector2 lTarget;
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    _locked += HSInput.MouseDelta * 0.004f;
                    _locked = Vector2.ClampMagnitude(_locked, 1f);
                    _locked = Vector2.Lerp(_locked, Vector2.zero, 1f - Mathf.Exp(-1.2f * Time.unscaledDeltaTime));
                    lTarget = _locked;
                }
                else
                {
                    Vector2 lMouse = HSInput.MousePosition;
                    lTarget = new Vector2(lMouse.x / Mathf.Max(1f, Screen.width) * 2f - 1f, lMouse.y / Mathf.Max(1f, Screen.height) * 2f - 1f);
                    lTarget = Vector2.ClampMagnitude(lTarget, 1.2f);
                }
                // Heavy and slow : the screens float with the mouse, they never jerk.
                _value = Vector2.Lerp(_value, lTarget, 1f - Mathf.Exp(-3.5f * Time.unscaledDeltaTime));
                return _value;
            }
        }
    }
}
