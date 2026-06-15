using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Domain;
using LifeCycle.Facade;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class GradualCombatApplier : IUpdateHandler
    {
        private struct PendingEffect
        {
            internal EntityId SourceId;
            internal EntityId TargetId;
            internal float RemainingTotal;
            internal float TickValue;
            internal float Elapsed;
            internal float Duration;
            internal float TickInterval;
            internal float TickAccumulator;
            internal bool IsDot;
            internal int CastLifecycleId;
        }

        private readonly DemoCombatRegistry _combatRegistry;
        private readonly DemoGameplaySession _session;
        private readonly IAbilityCastLifecycle _castLifecycle;
        private readonly List<PendingEffect> _pending = new List<PendingEffect>(8);

        public GradualCombatApplier(
            DemoCombatRegistry combatRegistry,
            DemoGameplaySession session,
            IAbilityCastLifecycle castLifecycle = null)
        {
            _combatRegistry = combatRegistry;
            _session = session;
            _castLifecycle = castLifecycle ?? NullAbilityCastLifecycle.Instance;
        }

        public void Enqueue(DamageRequestedEvent damageEvent)
        {
            float remaining = damageEvent.TotalValue > 0f ? damageEvent.TotalValue : damageEvent.TickValue;
            _pending.Add(new PendingEffect
            {
                SourceId = damageEvent.SourceId,
                TargetId = damageEvent.TargetId,
                RemainingTotal = remaining,
                TickValue = damageEvent.TickValue,
                Duration = damageEvent.ApplicationDuration,
                TickInterval = damageEvent.TickInterval > 0f ? damageEvent.TickInterval : DemoConstants.Combat.DefaultTickIntervalSeconds,
                IsDot = damageEvent.SourceId.Equals(default(EntityId)),
                CastLifecycleId = damageEvent.CastLifecycleId
            });
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_session.IsActive || _pending.Count == 0)
                return;

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingEffect effect = _pending[i];
                effect.Elapsed += deltaTime;
                effect.TickAccumulator += deltaTime;

                if (effect.Elapsed >= effect.Duration || effect.RemainingTotal <= 0f)
                {
                    CompleteCastEffect(effect.CastLifecycleId);
                    _pending.RemoveAt(i);
                    continue;
                }

                if (effect.TickAccumulator < effect.TickInterval)
                {
                    _pending[i] = effect;
                    continue;
                }

                effect.TickAccumulator = 0f;
                float amount = Math.Min(effect.TickValue, effect.RemainingTotal);
                if (amount > 0f && _combatRegistry.TryGet(effect.TargetId, out EntityCombatState combat))
                {
                    float tweenDuration = ComputeTweenDuration(effect.TickInterval);
                    if (effect.IsDot)
                        combat.ApplyDot(amount, tweenDuration);
                    else
                        combat.ApplyDamage(amount, tweenDuration);
                }

                effect.RemainingTotal -= amount;
                _pending[i] = effect;

                if (effect.RemainingTotal <= 0f)
                {
                    CompleteCastEffect(effect.CastLifecycleId);
                    _pending.RemoveAt(i);
                }
            }
        }

        public void CancelPendingForTarget(EntityId targetId)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (!_pending[i].TargetId.Equals(targetId))
                    continue;

                CompleteCastEffect(_pending[i].CastLifecycleId);
                _pending.RemoveAt(i);
            }
        }

        private void CompleteCastEffect(int castLifecycleId)
        {
            if (castLifecycleId <= 0)
                return;

            _castLifecycle.CompletePendingEffect(castLifecycleId);
        }

        public static float ComputeTweenDuration(float tickInterval) =>
            tickInterval > 0f
                ? Math.Min(tickInterval * DemoConstants.Combat.TweenDurationTickFactor, DemoConstants.Combat.MaxTweenDurationSeconds)
                : DemoConstants.Combat.DefaultTweenDurationSeconds;
    }
}
