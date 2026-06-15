using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class CombatEntityFeedbackWire
    {
        private readonly EntityCombatState _state;
        private readonly DemoEntityRegistry _registry;
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly CombatFeedbackPresenter _combatFeedback;
        private readonly EntityDespawnService _despawnService;
        private float _lastHealthBarVisualTime;

        public CombatEntityFeedbackWire(
            EntityCombatState state,
            DemoEntityRegistry registry,
            EntityStateRegistry entityStateRegistry,
            CombatFeedbackPresenter combatFeedback,
            EntityDespawnService despawnService = null)
        {
            _state = state;
            _registry = registry;
            _entityStateRegistry = entityStateRegistry;
            _combatFeedback = combatFeedback;
            _despawnService = despawnService;

            _state.HealthChanged += OnHealthChanged;
            _state.NumberRequested += OnNumberRequested;
        }

        private void OnHealthChanged(float current, float max, float tweenDuration)
        {
            float now = Time.unscaledTime;
            bool allowVisual = tweenDuration <= 0f ||
                               now - _lastHealthBarVisualTime >= DemoConstants.HealthBar.MinVisualUpdateIntervalSeconds ||
                               current <= 0f;

            if (allowVisual &&
                _registry.TryGetView(_state.EntityId, out EntityView view) &&
                view.HealthBar != null &&
                view.HealthBar.HasVisual)
            {
                view.HealthBar.SetFill(current, max, tweenDuration);
                _lastHealthBarVisualTime = now;
            }

            if (current <= 0f &&
                _entityStateRegistry.TryGet(_state.EntityId, out IEntityStateMachineController controller))
            {
                controller.TryTransition(EntityStatePaths.Vitality.Dead);
                _despawnService?.ScheduleDespawn(_state.EntityId);
            }
        }

        private void OnNumberRequested(float value, CombatNumberType type)
        {
            if (_registry.TryGetView(_state.EntityId, out EntityView view) && _combatFeedback != null)
                _combatFeedback.ShowNumber(view, value, type);
        }

        public void Dispose()
        {
            _state.HealthChanged -= OnHealthChanged;
            _state.NumberRequested -= OnNumberRequested;
        }
    }
}
