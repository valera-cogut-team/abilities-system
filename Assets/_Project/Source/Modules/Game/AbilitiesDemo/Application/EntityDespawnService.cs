using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using Cysharp.Threading.Tasks;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class EntityDespawnService
    {
        private readonly DemoEntityRegistry _registry;
        private readonly EntityStateRegistry _entityStateRegistry;
        private readonly DemoCombatRegistry _combatRegistry;
        private readonly TargetingService _targeting;
        private readonly GradualCombatApplier _gradualApplier;
        private readonly IAbilitiesFacade _abilities;
        private readonly EntityAttachedVfxRegistry _entityVfxRegistry;
        private readonly CombatFeedbackPresenter _combatFeedback;
        private readonly Dictionary<EntityId, CombatEntityFeedbackWire> _feedbackWires =
            new Dictionary<EntityId, CombatEntityFeedbackWire>();

        private DemoWorldUiCanvas _worldUi;
        private readonly HashSet<EntityId> _scheduledDespawns = new HashSet<EntityId>();

        public EntityDespawnService(
            DemoEntityRegistry registry,
            EntityStateRegistry entityStateRegistry,
            DemoCombatRegistry combatRegistry,
            TargetingService targeting,
            GradualCombatApplier gradualApplier,
            IAbilitiesFacade abilities,
            EntityAttachedVfxRegistry entityVfxRegistry,
            CombatFeedbackPresenter combatFeedback = null)
        {
            _registry = registry;
            _entityStateRegistry = entityStateRegistry;
            _combatRegistry = combatRegistry;
            _targeting = targeting;
            _gradualApplier = gradualApplier;
            _abilities = abilities;
            _entityVfxRegistry = entityVfxRegistry;
            _combatFeedback = combatFeedback;
        }

        public void BindWorldUi(DemoWorldUiCanvas worldUi) => _worldUi = worldUi;

        public void RegisterFeedbackWire(EntityId id, CombatEntityFeedbackWire wire)
        {
            if (id == default || wire == null)
                return;

            _feedbackWires[id] = wire;
        }

        public void ScheduleDespawn(EntityId id)
        {
            if (!CanDespawn(id) || !_scheduledDespawns.Add(id))
                return;

            ScheduleDespawnAsync(id).Forget();
        }

        public bool DespawnEnemy(EntityId id)
        {
            if (!CanDespawn(id))
                return false;

            _scheduledDespawns.Remove(id);
            _targeting.RemoveSelection(id);
            _gradualApplier.CancelPendingForTarget(id);
            _abilities.UnregisterEntity(id);

            if (_entityStateRegistry.TryGet(id, out IEntityStateMachineController controller))
            {
                controller.ReleasePresentationEffects();
                controller.Dispose();
            }

            _entityVfxRegistry?.ReleaseAll(id);
            _entityStateRegistry.Unregister(id);

            if (_feedbackWires.TryGetValue(id, out CombatEntityFeedbackWire wire))
            {
                wire.Dispose();
                _feedbackWires.Remove(id);
            }

            _combatRegistry.Remove(id);

            if (_registry.TryGetView(id, out EntityView view))
            {
                _combatFeedback?.CancelLabelsForEntity(id);

                if (view.HealthBar != null)
                {
                    view.HealthBar.Dispose();
                    view.AttachHealthBar(null);
                }

                DestroyObject(view.gameObject);
            }

            _registry.RemoveEnemy(id);
            return true;
        }

        private async UniTaskVoid ScheduleDespawnAsync(EntityId id)
        {
            try
            {
                if (DemoConstants.Combat.DeathDespawnDelaySeconds > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(DemoConstants.Combat.DeathDespawnDelaySeconds),
                        ignoreTimeScale: true);
                }

                DespawnEnemy(id);
            }
            catch (Exception)
            {
                _scheduledDespawns.Remove(id);
            }
        }

        private bool CanDespawn(EntityId id) =>
            id != default &&
            !_registry.PlayerId.Equals(id) &&
            _registry.IsEnemy(id);

        private static void DestroyObject(GameObject go)
        {
            if (go == null)
                return;

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(go);
                return;
            }
#endif
            UnityEngine.Object.Destroy(go);
        }
    }
}
