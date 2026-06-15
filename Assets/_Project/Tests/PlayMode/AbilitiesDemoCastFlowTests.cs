using System;
using System.Collections;
using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Facade;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests;
using Cysharp.Threading.Tasks;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using StateMachine.Application;
using StateMachine.Facade;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

namespace AvantajPrim.Tests.PlayMode
{
    public sealed class AbilitiesDemoCastFlowTests
    {
        [UnityTest]
        public IEnumerator CastFirewall_ReducesEnemyHealth_WhenEnemySelected()
        {
            var registry = new DemoEntityRegistry();
            var combatRegistry = new DemoCombatRegistry();
            var entityStateRegistry = new EntityStateRegistry();
            var targeting = new TargetingService();
            var session = new DemoGameplaySession(targeting, new PlayerMovementService(registry, entityStateRegistry));
            session.SetActive(true);

            EntityView playerView = CreateView(new EntityId(1), Vector3.zero, true);
            EntityView enemyView = CreateView(new EntityId(2), new Vector3(3f, 0f, 0f), false);
            registry.SetPlayer(new EntityId(1), playerView);
            registry.AddEnemy(new EntityId(2), enemyView);

            var playerCombat = new EntityCombatState(new EntityId(1), "Player", 100f);
            var enemyCombat = new EntityCombatState(new EntityId(2), "Enemy", 100f);
            combatRegistry.Register(playerCombat);
            combatRegistry.Register(enemyCombat);

            WireStateMachine(new EntityId(1), playerView, entityStateRegistry, combatRegistry, isPlayer: true);
            WireStateMachine(new EntityId(2), enemyView, entityStateRegistry, combatRegistry, isPlayer: false);

            var catalog = new AbilityCatalog();
            catalog.Register(new AbilityDefinition(
                new AbilityId(DemoConstants.AbilityIds.Firewall),
                new List<IAbilityComponentData>
                {
                    new DamageComponentData { TotalValue = 25f, PlayTimeType = AbilityPlayTimeType.OnStart }
                },
                hotkeySlot: 2,
                targetType: AbilityTargetType.Enemy,
                range: 20f));

            var abilities = new RecordingAbilitiesFacade(combatRegistry);
            var resolver = new DemoAbilityTargetResolver(registry, targeting);
            var movement = new PlayerMovementService(registry, entityStateRegistry);
            var casting = new AbilityCastingService(
                abilities,
                catalog,
                registry,
                entityStateRegistry,
                session,
                new PlayerInputRouter(targeting, movement, registry, entityStateRegistry),
                movement,
                resolver,
                logger: null);

            targeting.SetSingleSelection(new EntityId(2));
            float initialHealth = enemyCombat.CurrentHealth;

            yield return casting.CastAsync(DemoConstants.AbilityIds.Firewall).ToCoroutine();

            Assert.IsTrue(abilities.WasCastInvoked);
            Assert.Less(enemyCombat.CurrentHealth, initialHealth);

            UnityEngine.Object.Destroy(playerView.gameObject);
            UnityEngine.Object.Destroy(enemyView.gameObject);
        }

        [UnityTest]
        public IEnumerator CastAsync_RotatesPlayerTowardTarget_BeforeOffensiveCast()
        {
            var registry = new DemoEntityRegistry();
            var entityStateRegistry = new EntityStateRegistry();
            var targeting = new TargetingService();
            var session = new DemoGameplaySession(targeting, new PlayerMovementService(registry, entityStateRegistry));
            session.SetActive(true);

            EntityView playerView = CreateView(new EntityId(1), Vector3.zero, true);
            playerView.transform.rotation = Quaternion.identity;
            EntityView enemyView = CreateView(new EntityId(2), new Vector3(5f, 0f, 0f), false);
            registry.SetPlayer(new EntityId(1), playerView);
            registry.AddEnemy(new EntityId(2), enemyView);

            WireStateMachine(new EntityId(1), playerView, entityStateRegistry, new DemoCombatRegistry(), isPlayer: true);

            AbilityCatalog catalog = CreateOffensiveCatalog();
            var abilities = new RecordingAbilitiesFacade(new DemoCombatRegistry());
            AbilityCastingService casting = CreateCastingService(
                registry,
                entityStateRegistry,
                targeting,
                session,
                catalog,
                abilities);

            targeting.SetSingleSelection(new EntityId(2));

            yield return casting.CastAsync(DemoConstants.AbilityIds.Firewall).ToCoroutine();

            Vector3 forward = playerView.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Assert.Greater(Vector3.Dot(forward, Vector3.right), 0.99f, "Player should face the target on cast (L11).");

            UnityEngine.Object.Destroy(playerView.gameObject);
            UnityEngine.Object.Destroy(enemyView.gameObject);
        }

        [UnityTest]
        public IEnumerator CastAsync_ReturnsInvalidTarget_AfterEnemyDespawned()
        {
            var registry = new DemoEntityRegistry();
            var entityStateRegistry = new EntityStateRegistry();
            var combatRegistry = new DemoCombatRegistry();
            var targeting = new TargetingService();
            var session = new DemoGameplaySession(targeting, new PlayerMovementService(registry, entityStateRegistry));
            session.SetActive(true);
            var gradualApplier = new GradualCombatApplier(combatRegistry, session);

            EntityView playerView = CreateView(new EntityId(1), Vector3.zero, true);
            EntityView enemyView = CreateView(new EntityId(2), new Vector3(3f, 0f, 0f), false);
            registry.SetPlayer(new EntityId(1), playerView);
            registry.AddEnemy(new EntityId(2), enemyView);
            combatRegistry.Register(new EntityCombatState(new EntityId(2), "Enemy", 100f));
            targeting.SetSingleSelection(new EntityId(2));

            var despawn = new EntityDespawnService(
                registry,
                entityStateRegistry,
                combatRegistry,
                targeting,
                gradualApplier,
                new StubAbilitiesFacade(),
                new EntityAttachedVfxRegistry());

            Assert.IsTrue(despawn.DespawnEnemy(new EntityId(2)));

            AbilityCatalog catalog = CreateOffensiveCatalog();
            var abilities = new RecordingAbilitiesFacade(combatRegistry);
            AbilityCastingService casting = CreateCastingService(
                registry,
                entityStateRegistry,
                targeting,
                session,
                catalog,
                abilities);

            CastAbilityResult result = default;
            UniTask<CastAbilityResult> castTask = casting.CastAsync(DemoConstants.AbilityIds.Firewall);
            yield return castTask.ToCoroutine();
            result = castTask.GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(CastAbilityErrorCode.InvalidTarget, result.ErrorCode);
            Assert.IsFalse(abilities.WasCastInvoked);

            UnityEngine.Object.Destroy(playerView.gameObject);
        }

        [UnityTest]
        public IEnumerator CompletePhase_ReleasesMovementLock_AllowsMoveTo()
        {
            var registry = new DemoEntityRegistry();
            var entityStateRegistry = new EntityStateRegistry();
            var targeting = new TargetingService();
            var movement = new PlayerMovementService(registry, entityStateRegistry);
            var session = new DemoGameplaySession(targeting, movement);
            session.SetActive(true);

            EntityView playerView = CreateView(new EntityId(1), Vector3.zero, true);
            registry.SetPlayer(new EntityId(1), playerView);
            WireStateMachine(new EntityId(1), playerView, entityStateRegistry, new DemoCombatRegistry(), isPlayer: true);

            Assert.IsTrue(entityStateRegistry.TryGet(new EntityId(1), out IEntityStateMachineController controller));
            controller.AcquireCastInputLock(movement: true, rotation: true);
            Assert.IsTrue(controller.IsMovementBlocked);

            var phaseHandler = new CastPhasePresentationHandler(entityStateRegistry);
            var casterId = new EntityId(1);
            phaseHandler.HandlePhaseChanged(new AbilityPhaseChangedEvent(
                new AbilityId("firewall"), casterId, AbilityConstants.Phases.Start));
            phaseHandler.HandlePhaseChanged(new AbilityPhaseChangedEvent(
                new AbilityId("firewall"), casterId, AbilityConstants.Phases.End));
            phaseHandler.HandlePhaseChanged(new AbilityPhaseChangedEvent(
                new AbilityId("firewall"), casterId, AbilityConstants.Phases.Complete));

            Assert.IsFalse(controller.IsMovementBlocked);
            movement.MoveTo(new Vector3(5f, 0f, 0f));
            Assert.IsTrue(movement.IsMoving);

            UnityEngine.Object.Destroy(playerView.gameObject);
            yield return null;
        }

        private static AbilityCatalog CreateOffensiveCatalog()
        {
            var catalog = new AbilityCatalog();
            catalog.Register(new AbilityDefinition(
                new AbilityId(DemoConstants.AbilityIds.Firewall),
                new List<IAbilityComponentData>(),
                hotkeySlot: 2,
                targetType: AbilityTargetType.Enemy,
                range: 20f));
            return catalog;
        }

        private static AbilityCastingService CreateCastingService(
            DemoEntityRegistry registry,
            EntityStateRegistry entityStateRegistry,
            TargetingService targeting,
            DemoGameplaySession session,
            AbilityCatalog catalog,
            RecordingAbilitiesFacade abilities)
        {
            var resolver = new DemoAbilityTargetResolver(registry, targeting);
            var movement = new PlayerMovementService(registry, entityStateRegistry);
            var inputRouter = new PlayerInputRouter(
                targeting,
                movement,
                registry,
                entityStateRegistry);
            return new AbilityCastingService(
                abilities,
                catalog,
                registry,
                entityStateRegistry,
                session,
                inputRouter,
                movement,
                resolver,
                logger: null);
        }

        private static void WireStateMachine(
            EntityId id,
            EntityView view,
            EntityStateRegistry registry,
            DemoCombatRegistry combatRegistry,
            bool isPlayer)
        {
            TestPresentationFactory.CreateController(
                view,
                id,
                isPlayer,
                registry,
                combatRegistry);
        }

        private static EntityView CreateView(EntityId id, Vector3 position, bool isPlayer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = position;
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(id, isPlayer);
            return view;
        }

        private sealed class RecordingAbilitiesFacade : IAbilitiesFacade
        {
            private DemoCombatRegistry _combatRegistry;
            public bool WasCastInvoked { get; private set; }

            public RecordingAbilitiesFacade(DemoCombatRegistry combatRegistry) => _combatRegistry = combatRegistry;

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
                System.Threading.CancellationToken cancellationToken = default)
            {
                WasCastInvoked = true;
                if (_combatRegistry != null && _combatRegistry.TryGet(targetId, out EntityCombatState combat))
                    combat.ApplyDamage(25f);
                return UniTask.FromResult(CastAbilityResult.Ok());
            }

            public UniTask<CastAbilityResult> CastOnTargetsAsync(
                AbilityId abilityId,
                EntityId casterId,
                IReadOnlyList<EntityId> targetIds,
                System.Threading.CancellationToken cancellationToken = default)
            {
                WasCastInvoked = true;
                return UniTask.FromResult(CastAbilityResult.Ok());
            }
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
                UniTask.FromResult(CastAbilityResult.Ok());

            public UniTask<CastAbilityResult> CastOnTargetsAsync(
                AbilityId abilityId,
                EntityId casterId,
                IReadOnlyList<EntityId> targetIds,
                System.Threading.CancellationToken cancellationToken = default) =>
                UniTask.FromResult(CastAbilityResult.Ok());
        }
    }
}
