using System.Collections;
using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using AvantajPrim.Abilities.Infrastructure;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilitiesServiceTests
    {
        private AbilitiesService _service;
        private AbilityCatalog _catalog;
        private AbilityCastLifecycle _lifecycle;

        [SetUp]
        public void SetUp()
        {
            _lifecycle = new AbilityCastLifecycle();
            var registry = new AbilityComponentRegistry();
            registry.Register(new DamageComponentExecutor(_lifecycle));

            var presentation = new RecordingPresentationPort();
            var executor = new AbilityExecutor(registry, presentation, NullEntityStatePort.Instance);
            _catalog = new AbilityCatalog();
            _service = new AbilitiesService(_catalog, executor, new AbilityActivationLog(), new NullPhaseNotifier(), _lifecycle);
        }

        [Test]
        public void CastAsync_ReturnsUnknownAbility_WhenNotRegistered()
        {
            CastAbilityResult result = AwaitCast(_service.CastAsync(
                new AbilityId("missing"), new EntityId(1), new EntityId(2)));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.UnknownAbility, result.ErrorCode);
        }

        [Test]
        public void CastAsync_ReturnsInvalidCaster_WhenCasterNotRegistered()
        {
            RegisterAbility("fireball");
            CastAbilityResult result = AwaitCast(_service.CastAsync(
                new AbilityId("fireball"), new EntityId(1), new EntityId(2)));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.InvalidCaster, result.ErrorCode);
        }

        [UnityTest]
        public IEnumerator CastAsync_ReturnsOk_WhenAbilityAndCasterExist()
        {
            RegisterAbility("fireball");
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(1), isPlayer: true));

            CastAbilityResult result = default;
            yield return _service.CastAsync(
                new AbilityId("fireball"), new EntityId(1), new EntityId(2)).ToCoroutine(r => result = r);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.None, result.ErrorCode);
        }

        [UnityTest]
        public IEnumerator CastAsync_ReturnsAlreadyCasting_WhenTargetBusy()
        {
            RegisterLongRunningAbility("slow");
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(1), isPlayer: true));

            UniTask<CastAbilityResult> first = _service.CastAsync(new AbilityId("slow"), new EntityId(1), new EntityId(2));
            yield return WaitUntilCastActive(new EntityId(1));

            CastAbilityResult second = default;
            yield return _service.CastAsync(new AbilityId("slow"), new EntityId(1), new EntityId(2))
                .ToCoroutine(r => second = r);

            Assert.IsFalse(second.Success);
            Assert.AreEqual(CastAbilityErrorCode.AlreadyCasting, second.ErrorCode);

            _lifecycle.CompletePendingEffect(1);
            yield return first.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator HasActiveCasts_ReturnsTrue_WhileCastInProgress()
        {
            RegisterLongRunningAbility("slow");
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(1), isPlayer: true));

            Assert.IsFalse(_service.HasActiveCasts(new EntityId(1)));

            UniTask<CastAbilityResult> castTask = _service.CastAsync(new AbilityId("slow"), new EntityId(1), new EntityId(2));
            yield return WaitUntilCastActive(new EntityId(1));

            Assert.IsTrue(_service.HasActiveCasts(new EntityId(1)));

            _lifecycle.CompletePendingEffect(1);
            yield return castTask.ToCoroutine();
            Assert.IsFalse(_service.HasActiveCasts(new EntityId(1)));
        }

        [UnityTest]
        public IEnumerator CastAsync_AllowsConcurrentCasts_OnDifferentTargets()
        {
            RegisterLongRunningAbility("slow");
            RegisterAbility("instant");
            _service.RegisterEntity(new AbilityEntityModel(new EntityId(1), isPlayer: true));

            UniTask<CastAbilityResult> first = _service.CastAsync(new AbilityId("slow"), new EntityId(1), new EntityId(2));
            yield return WaitUntilCastActive(new EntityId(1));

            CastAbilityResult second = default;
            yield return _service.CastAsync(new AbilityId("instant"), new EntityId(1), new EntityId(3))
                .ToCoroutine(r => second = r);

            Assert.IsTrue(second.Success);
            Assert.AreEqual(CastAbilityErrorCode.None, second.ErrorCode);

            _lifecycle.CompletePendingEffect(1);
            yield return first.ToCoroutine();
        }

        private static CastAbilityResult AwaitCast(UniTask<CastAbilityResult> castTask) =>
            castTask.GetAwaiter().GetResult();

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

        private void RegisterAbility(string id)
        {
            var components = new List<IAbilityComponentData>
            {
                new DamageComponentData { PlayTimeType = AbilityPlayTimeType.OnStart, TotalValue = 5f }
            };
            _catalog.Register(new AbilityDefinition(new AbilityId(id), components));
        }

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

        private sealed class NullPhaseNotifier : IAbilityPhaseNotifier
        {
            public void NotifyPhaseChanged(AbilityPhaseChangedEvent evt) { }
        }
    }
}
