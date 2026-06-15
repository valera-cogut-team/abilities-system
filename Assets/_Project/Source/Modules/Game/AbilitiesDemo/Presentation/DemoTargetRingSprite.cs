using AvantajPrim.AbilitiesDemo.Domain;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    internal static class DemoTargetRingSprite
    {
        private static Sprite _ring;

        public static Sprite Ring
        {
            get
            {
                if (_ring != null)
                    return _ring;

                int textureSize = DemoConstants.Targeting.RingTextureSize;
                var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "TargetRingSprite"
                };

                float center = (textureSize - 1) * 0.5f;
                var pixels = new Color32[textureSize * textureSize];
                float outerRadius = DemoConstants.Targeting.RingOuterRadius;
                float innerRadius = DemoConstants.Targeting.RingInnerRadius;

                for (int y = 0; y < textureSize; y++)
                {
                    for (int x = 0; x < textureSize; x++)
                    {
                        float dx = (x - center) / textureSize;
                        float dy = (y - center) / textureSize;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        byte alpha = distance >= innerRadius && distance <= outerRadius ? (byte)255 : (byte)0;
                        pixels[y * textureSize + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                _ring = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, textureSize, textureSize),
                    new Vector2(0.5f, 0.5f),
                    textureSize);
                _ring.name = "TargetRingSprite";
                return _ring;
            }
        }
    }
}
