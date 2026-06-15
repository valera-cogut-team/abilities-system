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
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilitiesServiceCastOnTargetsTests
    {
        private AbilitiesService _service;
        private AbilityCatalog _catalog;
        private RecordingPresentationPort _presentation;
        private AbilityCastLifecycle _lifecycle;

        [SetUp]
        public void SetUp()
        {
            _lifecycle = new AbilityCastLifecycle();
            var registry = new AbilityComponentRegistry();
            registry.Register(new DamageComponentExecutor(_lifecycle));
            _presentation = new RecordingPresentationPort();
            var executor = new AbilityExecutor(registry, _presentation, NullEntityStatePort.Instance);
            _catalog = new AbilityCatalog();
            _service = new AbilitiesService(_catalog, executor, new AbilityActivationLog(), new NullPhaseNotifier(), _lifecycle);
        }

        [Test]
        public void CastOnTargetsAsync_ExecutesForMultipleTargets()
        {
            var components = new List<IAbilityComponentData>
            {
                new DamageComponentData { PlayTimeType = AbilityPlayTimeType.OnStart, TotalValue = 10f }
            };
            _catalog.Register(new AbilityDefinition(new AbilityId("aoe"), components));
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(1), isPlayer: true));
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(2), isPlayer: false));
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(3), isPlayer: false));

            CastAbilityResult result = _service.CastOnTargetsAsync(
                new AbilityId("aoe"),
                new EntityId(1),
                new List<EntityId> { new EntityId(2), new EntityId(3) }).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.GreaterOrEqual(_presentation.DamageEvents.Count, 2);
        }

        [UnityTest]
        public IEnumerator CastOnTargetsAsync_ReturnsAlreadyCasting_WhenAnyTargetBusy()
        {
            RegisterLongRunningAbility("slow");
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(1), isPlayer: true));

            UniTask<CastAbilityResult> first = _service.CastAsync(new AbilityId("slow"), new EntityId(1), new EntityId(2));
            yield return WaitUntilCastActive(new EntityId(1));

            CastAbilityResult multi = default;
            yield return _service.CastOnTargetsAsync(
                    new AbilityId("slow"),
                    new EntityId(1),
                    new List<EntityId> { new EntityId(2), new EntityId(3) })
                .ToCoroutine(r => multi = r);

            Assert.IsFalse(multi.Success);
            Assert.AreEqual(CastAbilityErrorCode.AlreadyCasting, multi.ErrorCode);

            _lifecycle.CompletePendingEffect(1);
            yield return first.ToCoroutine();
        }

        private static IEnumerator WaitUntilCastActive(AbilitiesService service, EntityId casterId)
        {
            const int maxFrames = 120;
            for (int i = 0; i < maxFrames; i++)
            {
                if (service.HasActiveCasts(casterId))
                    yield break;

                yield return null;
            }

            Assert.Fail("Cast did not reach waiting state.");
        }

        private IEnumerator WaitUntilCastActive(EntityId casterId) => WaitUntilCastActive(_service, casterId);

        private void RegisterLongRunningAbility(string id)
        {
            var components = new List<IAbilityComponentData>
            {
                new DamageComponentData
                {
                    PlayTimeType = AbilityPlayTimeType.OnStart,
                    TotalValue = 12f,
                    TickValue = 3f,
                    ApplicationDuration = 10f,
                    TickInterval = 0.5f
                }
            };
            _catalog.Register(new AbilityDefinition(new AbilityId(id), components));
        }

        [Test]
        public void CastOnTargetsAsync_ReturnsInvalidTarget_WhenEmpty()
        {
            CastAbilityResult result = _service.CastOnTargetsAsync(
                new AbilityId("aoe"),
                new EntityId(1),
                new List<EntityId>()).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.InvalidTarget, result.ErrorCode);
        }

        private sealed class NullPhaseNotifier : IAbilityPhaseNotifier
        {
            public void NotifyPhaseChanged(AbilityPhaseChangedEvent evt) { }
        }
    }
}
