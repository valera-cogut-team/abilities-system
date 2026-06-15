using AvantajPrim.AbilitiesDemo.Domain;
using TMPro;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    internal static class DemoTmpFontProvider
    {
        private static TMP_FontAsset _font;

        public static bool TryGetFont(out TMP_FontAsset font)
        {
            EnsureLoaded();
            font = _font;
            return font != null;
        }

        private static void EnsureLoaded()
        {
            if (_font != null)
                return;

            _font = TMP_Settings.defaultFontAsset;
            if (_font != null)
                return;

            _font = Resources.Load<TMP_FontAsset>(DemoConstants.Ui.TmpFontResourcePath);
            if (_font != null)
                return;

            _font = Resources.Load<TMP_FontAsset>(DemoConstants.Ui.TmpFontFallbackResourcePath);
        }

        public static void ApplyTo(TMP_Text text)
        {
            if (text == null || !TryGetFont(out TMP_FontAsset font))
                return;

            text.font = font;
            text.fontSharedMaterial = font.material;
        }
    }
}
