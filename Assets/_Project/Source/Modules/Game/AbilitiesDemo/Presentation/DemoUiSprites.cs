using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    internal static class DemoUiSprites
    {
        private static Sprite _white;

        public static Sprite White
        {
            get
            {
                if (_white != null)
                    return _white;

                Texture2D texture = Texture2D.whiteTexture;
                _white = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                return _white;
            }
        }
    }
}
