using System;
using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests.Shared;
using Cysharp.Threading.Tasks;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityCastingServiceTests
    {
        private AbilityCastingService _casting;
        private DemoEntityRegistry _registry;
        private EntityStateRegistry _entityStateRegistry;
        private AbilityCatalog _catalog;
        private TargetingService _targeting;
        private DemoGameplaySession _session;
        private PlayerInputRouter _inputRouter;
        private PlayerMovementService _movement;
        private RecordingAbilitiesFacade _abilities;

        [SetUp]
        public void SetUp()
        {
            _registry = new DemoEntityRegistry();
            _entityStateRegistry = new EntityStateRegistry();
            _catalog = new AbilityCatalog();
            _targeting = new TargetingService();
            _session = new DemoGameplaySession(_targeting, new PlayerMovementService(_registry, _entityStateRegistry));
            _session.SetActive(true);
            _abilities = new RecordingAbilitiesFacade();
            _movement = new PlayerMovementService(_registry, _entityStateRegistry);
            _inputRouter = new PlayerInputRouter(
                _targeting,
                _movement,
                _registry,
                _entityStateRegistry);
            RegisterDefaultAbilities();
            var targetResolver = new DemoAbilityTargetResolver(_registry, _targeting);
            _casting = new AbilityCastingService(
                _abilities,
                _catalog,
                _registry,
                _entityStateRegistry,
                _session,
                _inputRouter,
                _movement,
                targetResolver,
                logger: null);
        }

        private void RegisterDefaultAbilities()
        {
            _catalog.Register(new AbilityDefinition(
                new AbilityId(DemoConstants.AbilityIds.Healing),
                new List<IAbilityComponentData>(),
                hotkeySlot: 3,
                targetType: AbilityTargetType.Player,
                range: 0f));
            _catalog.Register(new AbilityDefinition(
                new AbilityId(DemoConstants.AbilityIds.Firewall),
                new List<IAbilityComponentData>(),
                hotkeySlot: 2,
                targetType: AbilityTargetType.Enemy,
                range: 12f));
            _catalog.Register(new AbilityDefinition(
                new AbilityId(DemoConstants.AbilityIds.Dash),
                new List<IAbilityComponentData>(),
                hotkeySlot: 1,
                targetType: AbilityTargetType.Enemy,
                range: 5f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (EntityView view in UnityEngine.Object.FindObjectsByType<EntityView>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(view.gameObject);
        }

        [Test]
        public void ResolveTargets_PlayerTarget_ReturnsCaster_IgnoringEnemySelection()
        {
            RegisterPlayerAndEnemies();
            _targeting.SetSingleSelection(new EntityId(3));

            List<EntityId> targets = _casting.ResolveTargets(new AbilityId(DemoConstants.AbilityIds.Healing));

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(new EntityId(1), targets[0]);
        }

        [Test]
        public void ResolveTargets_PlayerTarget_ReturnsCaster_WhenNoSelection()
        {
            RegisterPlayerAndEnemies();
            _targeting.ClearSelection();

            List<EntityId> targets = _casting.ResolveTargets(new AbilityId(DemoConstants.AbilityIds.Healing));

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(new EntityId(1), targets[0]);
        }

        [Test]
        public void ResolveTargets_EnemyTarget_ReturnsEmpty_WhenAbilityConfigUsesEnemyTarget()
        {
            RegisterPlayerAndEnemies();
            _targeting.ClearSelection();
            _catalog.Register(new AbilityDefinition(
                new AbilityId("player_ability_as_enemy_target"),
                new List<IAbilityComponentData>(),
                targetType: AbilityTargetType.Enemy));

            List<EntityId> targets = _casting.ResolveTargets(new AbilityId("player_ability_as_enemy_target"));

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void ResolveTarget_Offensive_UsesSelectedEnemy()
        {
            RegisterPlayerAndEnemies();
            _targeting.SetSingleSelection(new EntityId(4));

            EntityId target = _casting.ResolveTarget(new AbilityId(DemoConstants.AbilityIds.Firewall));

            Assert.AreEqual(new EntityId(4), target);
        }

        [Test]
        public void ResolveTargets_Offensive_ReturnsEmptyWhenNoSelection()
        {
            RegisterPlayerAndEnemies();
            _targeting.ClearSelection();

            List<EntityId> targets = _casting.ResolveTargets(new AbilityId(DemoConstants.AbilityIds.Firewall));

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void ResolveTarget_Offensive_ReturnsDefaultWhenNoSelection()
        {
            RegisterPlayerAndEnemies();
            _targeting.ClearSelection();

            EntityId target = _casting.ResolveTarget(new AbilityId(DemoConstants.AbilityIds.Dash));

            Assert.AreEqual(default(EntityId), target);
        }

        [Test]
        public void CastAsync_ReturnsInvalidTarget_WhenNoEnemySelected()
        {
            RegisterPlayerAndEnemies();
            _targeting.ClearSelection();

            CastAbilityResult result = _casting.CastAsync(DemoConstants.AbilityIds.Firewall).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.InvalidTarget, result.ErrorCode);
            Assert.IsFalse(_abilities.WasCastInvoked);
        }

        [Test]
        public void CastAsync_ReturnsInvalidTarget_AfterSelectionCleared()
        {
            RegisterPlayerAndEnemies();
            _targeting.SetSingleSelection(new EntityId(3));
            _targeting.ClearSelection();

            CastAbilityResult result = _casting.CastAsync(DemoConstants.AbilityIds.Dash).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.InvalidTarget, result.ErrorCode);
            Assert.IsFalse(_abilities.WasCastInvoked);
        }

        [Test]
        public void CastAsync_PlayerTarget_SucceedsOnCaster_WhenNoSelection()
        {
            RegisterPlayerAndEnemies();
            _targeting.ClearSelection();

            CastAbilityResult result = _casting.CastAsync(DemoConstants.AbilityIds.Healing).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.IsTrue(_abilities.UsedSingleTargetCast);
            Assert.AreEqual(new EntityId(1), _abilities.LastTarget);
        }

        [Test]
        public void CastAsync_PlayerTarget_CastsOnCaster_WhenEnemiesSelected()
        {
            RegisterPlayerAndEnemies();
            _targeting.SetSingleSelection(new EntityId(2));
            _targeting.ToggleSelection(new EntityId(4));

            List<EntityId> resolved = _casting.ResolveTargets(new AbilityId(DemoConstants.AbilityIds.Healing));
            Assert.AreEqual(1, resolved.Count);
            Assert.AreEqual(new EntityId(1), resolved[0]);

            CastAbilityResult result = _casting.CastAsync(DemoConstants.AbilityIds.Healing).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success, $"Expected success but got {result.ErrorCode}.");
            Assert.IsTrue(_abilities.UsedSingleTargetCast);
            Assert.IsFalse(_abilities.UsedMultiTargetCast);
            Assert.AreEqual(new EntityId(1), _abilities.LastTarget);
        }

        [Test]
        public void CastAsync_EnemyTarget_Fails_WhenNoSelection_RegardlessOfAbilityId()
        {
            RegisterPlayerAndEnemies();
            _targeting.ClearSelection();
            const string abilityId = "healing_as_enemy_target";
            _catalog.Register(new AbilityDefinition(
                new AbilityId(abilityId),
                new List<IAbilityComponentData>(),
                targetType: AbilityTargetType.Enemy));

            CastAbilityResult result = _casting.CastAsync(abilityId).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.InvalidTarget, result.ErrorCode);
            Assert.IsFalse(_abilities.WasCastInvoked);
        }

        [Test]
        public void CastAsync_UsesCastOnTargetsAsync_WhenMultiSelectOffensive()
        {
            RegisterPlayerAndEnemies();
            _targeting.SetSingleSelection(new EntityId(3));
            _targeting.ToggleSelection(new EntityId(4));

            CastAbilityResult result = _casting.CastAsync(DemoConstants.AbilityIds.Firewall).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.IsTrue(_abilities.UsedMultiTargetCast);
            Assert.AreEqual(2, _abilities.LastTargets.Count);
            Assert.Contains(new EntityId(3), _abilities.LastTargets);
            Assert.Contains(new EntityId(4), _abilities.LastTargets);
        }

        [Test]
        public void ResolveTargets_Offensive_UsesAllSelectedEnemies()
        {
            RegisterPlayerAndEnemies();
            _targeting.SetSingleSelection(new EntityId(3));
            _targeting.ToggleSelection(new EntityId(4));

            List<EntityId> targets = _casting.ResolveTargets(new AbilityId(DemoConstants.AbilityIds.Firewall));

            Assert.AreEqual(2, targets.Count);
            Assert.Contains(new EntityId(3), targets);
            Assert.Contains(new EntityId(4), targets);
        }

        [Test]
        public void CastAsync_PassesResolvedTargetToFacade()
        {
            RegisterPlayerAndEnemies();
            _catalog.Register(new AbilityDefinition(
                new AbilityId(DemoConstants.AbilityIds.Firewall),
                new List<IAbilityComponentData>
                {
                    TestAddressableRefs.CreateAnimationComponent(TestAddressableRefs.AnimDashPath)
                },
                hotkeySlot: 2,
                targetType: AbilityTargetType.Enemy,
                range: 12f));
            _targeting.SetSingleSelection(new EntityId(3));

            _casting.CastAsync(DemoConstants.AbilityIds.Firewall).GetAwaiter().GetResult();

            Assert.AreEqual(new EntityId(1), _abilities.LastCaster);
            Assert.AreEqual(new EntityId(3), _abilities.LastTarget);
        }

        private void RegisterPlayerAndEnemies()
        {
            EntityView player = CreateView(new EntityId(1), Vector3.zero, "Player");
            _registry.SetPlayer(new EntityId(1), player);

            _registry.AddEnemy(new EntityId(2), CreateView(new EntityId(2), new Vector3(2f, 0f, 0f), "Enemy A"));
            _registry.AddEnemy(new EntityId(3), CreateView(new EntityId(3), new Vector3(8f, 0f, 0f), "Enemy B"));
            _registry.AddEnemy(new EntityId(4), CreateView(new EntityId(4), new Vector3(4f, 0f, 2f), "Enemy C"));
        }

        private static EntityView CreateView(EntityId id, Vector3 position, string name)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(id, id.Value == 1, name);
            return view;
        }

        private sealed class RecordingAbilitiesFacade : IAbilitiesFacade
        {
            public EntityId LastCaster;
            public EntityId LastTarget;
            public List<EntityId> LastTargets = new List<EntityId>();
            public bool UsedSingleTargetCast;
            public bool UsedMultiTargetCast;

            public bool WasCastInvoked => UsedSingleTargetCast || UsedMultiTargetCast;

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

            public UniTask<CastAbilityResult> CastAsync(AbilityId abilityId, EntityId casterId, EntityId targetId,
                System.Threading.CancellationToken cancellationToken = default)
            {
                UsedMultiTargetCast = false;
                UsedSingleTargetCast = true;
                LastCaster = casterId;
                LastTarget = targetId;
                LastTargets.Clear();
                LastTargets.Add(targetId);
                return UniTask.FromResult(CastAbilityResult.Ok());
            }

            public UniTask<CastAbilityResult> CastOnTargetsAsync(AbilityId abilityId, EntityId casterId,
                IReadOnlyList<EntityId> targetIds, System.Threading.CancellationToken cancellationToken = default)
            {
                UsedSingleTargetCast = false;
                UsedMultiTargetCast = true;
                LastCaster = casterId;
                LastTargets.Clear();
                if (targetIds != null)
                    LastTargets.AddRange(targetIds);
                LastTarget = LastTargets.Count > 0 ? LastTargets[0] : default;
                return UniTask.FromResult(CastAbilityResult.Ok());
            }
        }
    }
}
