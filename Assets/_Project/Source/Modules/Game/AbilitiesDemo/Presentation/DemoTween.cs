using System;
using DG.Tweening;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    internal static class DemoTween
    {
        public static int Id(GameObject owner) => owner.GetInstanceID();

        public static int Id(Component owner) => owner.gameObject.GetInstanceID();

        public static void Kill(int id, bool complete = false) => DOTween.Kill(id, complete);

        public static Tweener ToFloat(
            int id,
            float from,
            float to,
            float duration,
            Action<float> onUpdate,
            Ease ease = Ease.OutQuad,
            GameObject linkTarget = null)
        {
            float value = from;
            Tweener tweener = DOTween.To(() => value, v =>
                {
                    value = v;
                    onUpdate(v);
                }, to, duration)
                .SetEase(ease)
                .SetRecyclable(true)
                .SetId(id);

            if (linkTarget != null)
                tweener.SetLink(linkTarget);

            return tweener;
        }

        public static Tweener Move(Transform target, Vector3 endValue, float duration, int id, Ease ease = Ease.OutQuad) =>
            target.DOMove(endValue, duration).SetEase(ease).SetRecyclable(true).SetId(id);

        public static Tweener Scale(Transform target, Vector3 endValue, float duration, int id, Ease ease = Ease.OutQuad) =>
            target.DOScale(endValue, duration).SetEase(ease).SetRecyclable(true).SetId(id);

        public static Tweener PunchScale(Transform target, Vector3 punch, float duration, int id, int vibrato = 6, float elasticity = 0.5f) =>
            target.DOPunchScale(punch, duration, vibrato, elasticity).SetRecyclable(true).SetId(id);
    }
}
