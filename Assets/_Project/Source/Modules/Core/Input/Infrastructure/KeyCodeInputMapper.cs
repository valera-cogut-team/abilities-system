using UnityEngine;
using UnityEngine.InputSystem;

namespace Input.Infrastructure
{
    internal static class KeyCodeInputMapper
    {
        internal static bool TryMap(int keyCodeValue, out Key key)
        {
            key = Key.None;
            if (keyCodeValue == 0)
                return false;

            var keyCode = (KeyCode)keyCodeValue;

            if (keyCode == KeyCode.Alpha0)
            {
                key = Key.Digit0;
                return true;
            }

            if (keyCode >= KeyCode.Alpha1 && keyCode <= KeyCode.Alpha9)
            {
                key = Key.Digit1 + (keyCode - KeyCode.Alpha1);
                return true;
            }

            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                key = Key.A + (keyCode - KeyCode.A);
                return true;
            }

            if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
            {
                key = Key.Numpad0 + (keyCode - KeyCode.Keypad0);
                return true;
            }

            if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F12)
            {
                key = Key.F1 + (keyCode - KeyCode.F1);
                return true;
            }

            switch (keyCode)
            {
                case KeyCode.LeftShift: key = Key.LeftShift; return true;
                case KeyCode.RightShift: key = Key.RightShift; return true;
                case KeyCode.LeftControl: key = Key.LeftCtrl; return true;
                case KeyCode.RightControl: key = Key.RightCtrl; return true;
                case KeyCode.LeftAlt: key = Key.LeftAlt; return true;
                case KeyCode.RightAlt: key = Key.RightAlt; return true;
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.Escape: key = Key.Escape; return true;
                case KeyCode.Tab: key = Key.Tab; return true;
                default: return false;
            }
        }
    }
}
