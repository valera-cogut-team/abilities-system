using AvantajPrim.AbilitiesDemo.Domain;
using DG.Tweening;
using Pool.Domain;
using TMPro;
using UnityEngine;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class PooledCombatLabel : IPoolable
    {
        private readonly CombatFeedbackPresenter _owner;

        public GameObject GameObject { get; }
        public TextMeshPro Label { get; }
        public int TweenId { get; }
        public EntityId AttachedEntityId { get; internal set; }

        public PooledCombatLabel(GameObject gameObject, TextMeshPro label, CombatFeedbackPresenter owner)
        {
            GameObject = gameObject;
            Label = label;
            _owner = owner;
            TweenId = DemoTween.Id(gameObject);
        }

        public void Reset()
        {
            DemoTween.Kill(TweenId);
            AttachedEntityId = default;

            if (Label != null)
            {
                Label.alpha = 1f;
                Label.transform.localPosition = Vector3.zero;
                Label.transform.localScale = Vector3.one * DemoConstants.CombatFeedback.LabelWorldScale;
            }

            GameObject.SetActive(false);
        }

        public void CompleteTween() => _owner?.ReturnLabel(this);
    }
}
