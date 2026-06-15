using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Presentation;
using Cysharp.Threading.Tasks;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class EntityDespawnServiceTests
    {
        private DemoEntityRegistry _registry;
        private EntityStateRegistry _entityStateRegistry;
        private DemoCombatRegistry _combatRegistry;
        private TargetingService _targeting;
        private GradualCombatApplier _gradualApplier;
        private EntityAttachedVfxRegistry _entityVfxRegistry;
        private EntityDespawnService _despawn;

        [SetUp]
        public void SetUp()
        {
            _registry = new DemoEntityRegistry();
            _entityStateRegistry = new EntityStateRegistry();
            _combatRegistry = new DemoCombatRegistry();
            _targeting = new TargetingService();
            _gradualApplier = new GradualCombatApplier(_combatRegistry, new DemoGameplaySession(_targeting, new PlayerMovementService(_registry, _entityStateRegistry)));
            _entityVfxRegistry = new EntityAttachedVfxRegistry();
            _despawn = new EntityDespawnService(
                _registry,
                _entityStateRegistry,
                _combatRegistry,
                _targeting,
                _gradualApplier,
                new StubAbilitiesFacade(),
                _entityVfxRegistry);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (EntityView view in UnityEngine.Object.FindObjectsByType<EntityView>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(view.gameObject);
        }

        [Test]
        public void DespawnEnemy_RemovesFromRegistries_AndClearsSelection()
        {
            var enemyId = new EntityId(2);
            var enemyGo = new GameObject("Enemy");
            EntityView enemyView = enemyGo.AddComponent<EntityView>();
            enemyView.Configure(enemyId, isPlayer: false);

            _registry.SetPlayer(new EntityId(1), CreateView(new EntityId(1), "Player"));
            _registry.AddEnemy(enemyId, enemyView);
            _combatRegistry.Register(new EntityCombatState(enemyId, "Enemy", 100f));
            _targeting.SetSingleSelection(enemyId);

            Assert.IsTrue(_despawn.DespawnEnemy(enemyId));
            Assert.IsFalse(_registry.IsEnemy(enemyId));
            Assert.IsFalse(_combatRegistry.TryGet(enemyId, out _));
            Assert.IsFalse(_entityStateRegistry.TryGet(enemyId, out _));
            Assert.AreEqual(0, _targeting.SelectedTargets.Count);
            Assert.IsTrue(enemyView == null);
        }

        [Test]
        public void DespawnEnemy_DoesNotDespawnPlayer()
        {
            var playerId = new EntityId(1);
            EntityView playerView = CreateView(playerId, "Player");
            _registry.SetPlayer(playerId, playerView);
            _combatRegistry.Register(new EntityCombatState(playerId, "Player", 100f));

            Assert.IsFalse(_despawn.DespawnEnemy(playerId));
            Assert.AreEqual(playerId, _registry.PlayerId);
        }

        [Test]
        public void ResolveTargets_ExcludesDespawnedEnemy()
        {
            var enemyId = new EntityId(2);
            EntityView enemyView = CreateView(enemyId, "Enemy");
            _registry.SetPlayer(new EntityId(1), CreateView(new EntityId(1), "Player"));
            _registry.AddEnemy(enemyId, enemyView);
            _combatRegistry.Register(new EntityCombatState(enemyId, "Enemy", 100f));
            _targeting.SetSingleSelection(enemyId);

            var resolver = new DemoAbilityTargetResolver(_registry, _targeting);
            _despawn.DespawnEnemy(enemyId);

            List<EntityId> targets = resolver.ResolveTargets(
                new AbilityId("firewall"),
                new AvantajPrim.Abilities.Execution.AbilityDefinition(
                    new AbilityId("firewall"),
                    new System.Collections.Generic.List<IAbilityComponentData>(),
                    targetType: AbilityTargetType.Enemy));

            Assert.AreEqual(0, targets.Count);
        }

        private static EntityView CreateView(EntityId id, string name)
        {
            var go = new GameObject(name);
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(id, id.Value == 1, name);
            return view;
        }

        private sealed class StubAbilitiesFacade : IAbilitiesFacade
        {
            public IObservable<AbilityPhaseChangedEvent> PhaseChanged => Observable.Empty<AbilityPhaseChangedEvent>();
            public IObservable<PresentationAnimationIntent> AnimationIntents => Observable.Empty<PresentationAnimationIntent>();
            public IObservable<PresentationSoundIntent> SoundIntents => Observable.Empty<PresentationSoundIntent>();
            public IObservable<PresentationVfxIntent> VfxIntents => Observable.Empty<PresentationVfxIntent>();
            public IObservable<PresentationMovementIntent> MovementIntents => Observable.Empty<PresentationMovementIntent>();
            public IObservable<PresentationAimIntent> AimIntents => Observable.Empty<PresentationAimIntent>();
            public IObservable<DamageRequestedEvent> DamageEvents => Observable.Empty<DamageRequestedEvent>();

            public void RegisterEntity(IAbilityEntity entity) { }

            public void UnregisterEntity(EntityId id) { }

            public bool HasActiveCasts(EntityId casterId) => false;

            public void OnUpdate(float deltaTime) { }

            public UniTask<CastAbilityResult> CastAsync(
                AbilityId abilityId,
                EntityId casterId,
                EntityId targetId,
                System.Threading.CancellationToken cancellationToken = default) =>
                UniTask.FromResult(CastAbilityResult.Fail(CastAbilityErrorCode.UnknownAbility));

            public UniTask<CastAbilityResult> CastOnTargetsAsync(
                AbilityId abilityId,
                EntityId casterId,
                System.Collections.Generic.IReadOnlyList<EntityId> targetIds,
                System.Threading.CancellationToken cancellationToken = default) =>
                UniTask.FromResult(CastAbilityResult.Fail(CastAbilityErrorCode.UnknownAbility));
        }
    }
}
