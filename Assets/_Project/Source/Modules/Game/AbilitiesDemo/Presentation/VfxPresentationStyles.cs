using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    internal static class VfxPresentationStyles
    {
        public static void ApplyHealingStyle(Transform root, float scaleMultiplier, float alphaMultiplier)
        {
            if (root == null)
                return;

            root.localScale *= scaleMultiplier;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ApplyAlphaMultiplier(systems[i], alphaMultiplier);
            }
        }

        private static void ApplyAlphaMultiplier(ParticleSystem particleSystem, float alphaMultiplier)
        {
            if (particleSystem == null || alphaMultiplier >= 1f)
                return;

            ParticleSystem.MainModule main = particleSystem.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            switch (startColor.mode)
            {
                case ParticleSystemGradientMode.Color:
                    Color color = startColor.color;
                    color.a *= alphaMultiplier;
                    main.startColor = color;
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    Color min = startColor.colorMin;
                    Color max = startColor.colorMax;
                    min.a *= alphaMultiplier;
                    max.a *= alphaMultiplier;
                    main.startColor = new ParticleSystem.MinMaxGradient(min, max);
                    break;
            }

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            ParticleSystem.MinMaxCurve rate = emission.rateOverTime;
            emission.rateOverTime = rate.constant * alphaMultiplier;
        }
    }
}
