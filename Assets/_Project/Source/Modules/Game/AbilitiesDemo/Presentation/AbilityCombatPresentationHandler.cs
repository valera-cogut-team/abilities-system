using AvantajPrim.Abilities.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class AbilityCombatPresentationHandler
    {
        private readonly DemoEntityRegistry _registry;
        private readonly DemoCombatRegistry _combatRegistry;
        private readonly GradualCombatApplier _gradualApplier;

        public AbilityCombatPresentationHandler(
            DemoEntityRegistry registry,
            DemoCombatRegistry combatRegistry,
            GradualCombatApplier gradualApplier)
        {
            _registry = registry;
            _combatRegistry = combatRegistry;
            _gradualApplier = gradualApplier;
        }

        public void HandleDamageEvent(DamageRequestedEvent damageEvent)
        {
            if (damageEvent.IsGradual)
            {
                _gradualApplier?.Enqueue(damageEvent);
                return;
            }

            if (!_combatRegistry.TryGet(damageEvent.TargetId, out EntityCombatState combat))
                return;

            float tweenDuration = GradualCombatApplier.ComputeTweenDuration(0f);
            if (damageEvent.SourceId.Equals(default(EntityId)))
                combat.ApplyDot(damageEvent.Value, tweenDuration);
            else
                combat.ApplyDamage(damageEvent.Value, tweenDuration);
        }

        public void HandleAimIntent(PresentationAimIntent intent)
        {
            if (!_registry.TryGetView(intent.CasterId, out EntityView casterView))
                return;

            if (!_registry.TryGetView(intent.TargetId, out EntityView targetView))
                return;

            Vector3 direction = targetView.transform.position - casterView.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < DemoConstants.Physics.DirectionEpsilonSqr)
                return;

            casterView.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
