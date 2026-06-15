using System.Collections;
using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Abilities.Infrastructure;
using AvantajPrim.Tests.EditMode;
using Cysharp.Threading.Tasks;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AvantajPrim.Tests.EditMode.Integration
{
    [TestFixture]
    public sealed class AbilitiesServiceCastLifecycleIntegrationTests
    {
        [UnityTest]
        public IEnumerator CastAsync_CompletesWhenOnEndStatusTransitionFails_FirewallShape()
        {
            var lifecycle = new AbilityCastLifecycle();
            var registry = CreateRegistry(lifecycle);
            var entityState = new SelectiveEntityStatePort(new EntityId(2));
            var presentation = new RecordingPresentationPort();
            var phaseNotifier = new RecordingPhaseNotifier();
            var executor = new AbilityExecutor(registry, presentation, entityState);
            var catalog = new AbilityCatalog();
            catalog.Register(CreateFirewallShapedDefinition());

            var service = new AbilitiesService(
                catalog,
                executor,
                new AbilityActivationLog(),
                phaseNotifier,
                lifecycle);

            var casterId = new EntityId(1);
            var targetId = new EntityId(2);
            service.RegisterEntity(new AbilityEntityModel(casterId, isPlayer: true));
            service.RegisterEntity(new AbilityEntityModel(targetId, isPlayer: false));

            CastAbilityResult result = default;
            yield return service.CastAsync(new AbilityId("firewall"), casterId, targetId)
                .ToCoroutine(r => result = r);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(phaseNotifier.ContainsPhase(AbilityConstants.Phases.Complete));
            Assert.IsFalse(service.HasActiveCasts(casterId));
        }

        [UnityTest]
        public IEnumerator CastAsync_EmitsCompletePhase_WhenNoPendingEffects()
        {
            var lifecycle = new AbilityCastLifecycle();
            var registry = CreateRegistry(lifecycle);
            var entityState = new RecordingEntityStatePort();
            var presentation = new RecordingPresentationPort();
            var phaseNotifier = new RecordingPhaseNotifier();
            var executor = new AbilityExecutor(registry, presentation, entityState);
            var catalog = new AbilityCatalog();
            catalog.Register(new AbilityDefinition(
                new AbilityId("instant"),
                new List<IAbilityComponentData>
                {
                    new DamageComponentData { PlayTimeType = AbilityPlayTimeType.OnStart, TotalValue = 10f }
                }));

            var service = new AbilitiesService(
                catalog,
                executor,
                new AbilityActivationLog(),
                phaseNotifier,
                lifecycle);

            var casterId = new EntityId(1);
            service.RegisterEntity(new AbilityEntityModel(casterId, isPlayer: true));
            service.RegisterEntity(new AbilityEntityModel(new EntityId(2), isPlayer: false));

            yield return service.CastAsync(new AbilityId("instant"), casterId, new EntityId(2)).ToCoroutine();

            Assert.IsTrue(phaseNotifier.ContainsPhase(AbilityConstants.Phases.Start));
            Assert.IsTrue(phaseNotifier.ContainsPhase(AbilityConstants.Phases.End));
            Assert.IsTrue(phaseNotifier.ContainsPhase(AbilityConstants.Phases.Complete));
        }

        private static AbilityComponentRegistry CreateRegistry(AbilityCastLifecycle lifecycle)
        {
            var registry = new AbilityComponentRegistry();
            registry.Register(new DamageComponentExecutor(lifecycle));
            registry.Register(new StatusEffectComponentExecutor(lifecycle));
            return registry;
        }

        private static AbilityDefinition CreateFirewallShapedDefinition()
        {
            return new AbilityDefinition(
                new AbilityId("firewall"),
                new List<IAbilityComponentData>
                {
                    new DamageComponentData
                    {
                        PlayTimeType = AbilityPlayTimeType.Delay,
                        DelaySeconds = 0f,
                        TotalValue = 25f
                    },
                    new StatusEffectComponentData
                    {
                        PlayTimeType = AbilityPlayTimeType.OnEnd,
                        EffectType = StatusEffectType.Combustion,
                        TargetType = AbilityTargetType.Enemy,
                        DurationType = AbilityDurationType.Continuous,
                        Duration = 4f,
                        Value = 8f,
                        TickInterval = 1f
                    }
                });
        }

        private sealed class RecordingPhaseNotifier : IAbilityPhaseNotifier
        {
            private readonly List<string> _phases = new List<string>();

            public void NotifyPhaseChanged(AbilityPhaseChangedEvent evt) =>
                _phases.Add(evt.PhaseName);

            public bool ContainsPhase(string phaseName) => _phases.Contains(phaseName);
        }
    }
}
