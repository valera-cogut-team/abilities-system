using Effects.Facade;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests.EditMode;
using Cysharp.Threading.Tasks;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityVfxPresenterTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (EntityView view in Object.FindObjectsByType<EntityView>(FindObjectsSortMode.None))
                Object.DestroyImmediate(view.gameObject);

            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name == "VfxStub")
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HandleVfxIntent_DoesNothing_WhenTargetViewMissing()
        {
            var effects = new RecordingEffectsFacade();
            var registry = new DemoEntityRegistry();
            var presenter = CreatePresenter(registry, effects);

            presenter.HandleVfxIntent(CreateIntent(new EntityId(99), "vfx_fire")).GetAwaiter().GetResult();

            Assert.AreEqual(0, effects.SpawnedKeys.Count);
        }

        [Test]
        public void HandleVfxIntent_SpawnsAtWorldPosition_ForDefaultStyle()
        {
            var effects = new RecordingEffectsFacade();
            var registry = new DemoEntityRegistry();
            EntityView view = CreateView(new EntityId(2), new Vector3(3f, 0f, 0f));
            registry.AddEnemy(new EntityId(2), view);
            var presenter = CreatePresenter(registry, effects);
            var intent = new PresentationVfxIntent(
                new EntityId(2),
                "vfx_hit",
                VfxPresentationStyle.Default,
                AbilityTargetType.Enemy,
                AbilityDurationType.Continuous,
                duration: 2f,
                delay: 0f,
                ox: 0f,
                oy: 1f,
                oz: 0f);

            presenter.HandleVfxIntent(intent).GetAwaiter().GetResult();

            Assert.AreEqual(1, effects.SpawnedKeys.Count);
            Assert.AreEqual("vfx_hit", effects.SpawnedKeys[0]);
            Assert.AreEqual(1, effects.ScheduledDespawns.Count);
        }

        [Test]
        public void HandleVfxIntent_AttachesToEntity_ForHealingStyle()
        {
            var effects = new RecordingEffectsFacade();
            var registry = new DemoEntityRegistry();
            EntityView view = CreateView(new EntityId(1), Vector3.zero, isPlayer: true);
            registry.SetPlayer(new EntityId(1), view);
            var vfxRegistry = new EntityAttachedVfxRegistry();
            vfxRegistry.BindEffects(effects);
            var presenter = new AbilityVfxPresenter(
                registry,
                effects,
                new StubAddressablesFacade(),
                logger: null,
                vfxRegistry);
            var intent = new PresentationVfxIntent(
                new EntityId(1),
                "vfx_healing",
                VfxPresentationStyle.Healing,
                AbilityTargetType.Player,
                AbilityDurationType.Continuous,
                duration: 3f,
                delay: 0f,
                ox: 0f,
                oy: 0f,
                oz: 0f);

            presenter.HandleVfxIntent(intent).GetAwaiter().GetResult();

            Assert.AreEqual(1, effects.SpawnedKeys.Count);
            Assert.AreEqual(view.transform, effects.NextSpawnResult.transform.parent);
        }

        [Test]
        public void PreloadVfxAsync_SkipsNullCatalog()
        {
            var effects = new RecordingEffectsFacade();
            var registry = new DemoEntityRegistry();
            var presenter = CreatePresenter(registry, effects);

            Assert.DoesNotThrow(() =>
                presenter.PreloadVfxAsync(null).GetAwaiter().GetResult());

            Assert.AreEqual(0, effects.PrewarmCalls.Count);
        }

        [Test]
        public void HandleVfxIntent_DoesNothing_WhenTargetViewDestroyed()
        {
            var effects = new RecordingEffectsFacade();
            var registry = new DemoEntityRegistry();
            EntityView view = CreateView(new EntityId(2), Vector3.zero);
            registry.AddEnemy(new EntityId(2), view);
            Object.DestroyImmediate(view.gameObject);
            var presenter = CreatePresenter(registry, effects);

            presenter.HandleVfxIntent(CreateIntent(new EntityId(2), "vfx_defenced_attack")).GetAwaiter().GetResult();

            Assert.AreEqual(0, effects.SpawnedKeys.Count);
        }

        [Test]
        public void HandleVfxIntent_SpawnsAtCapturedWorldPosition_WhenTargetDestroyedDuringAsyncSpawn()
        {
            var effects = new RecordingEffectsFacade();
            var registry = new DemoEntityRegistry();
            EntityView view = CreateView(new EntityId(2), new Vector3(5f, 1f, 2f));
            registry.AddEnemy(new EntityId(2), view);
            var presenter = CreatePresenter(registry, effects);
            effects.BeforeCompleteSpawn = () => Object.DestroyImmediate(view.gameObject);

            presenter.HandleVfxIntent(CreateIntent(new EntityId(2), "vfx_defenced_attack")).GetAwaiter().GetResult();

            Assert.AreEqual(1, effects.SpawnedKeys.Count);
            Assert.AreEqual(new Vector3(5f, 1f, 2f), effects.LastSpawnWorldPosition);
        }

        [Test]
        public void HandleVfxIntent_DoesNothing_WhenPrefabKeyEmpty()
        {
            var effects = new RecordingEffectsFacade();
            var registry = new DemoEntityRegistry();
            EntityView view = CreateView(new EntityId(2), Vector3.zero);
            registry.AddEnemy(new EntityId(2), view);
            var presenter = CreatePresenter(registry, effects);

            presenter.HandleVfxIntent(CreateIntent(new EntityId(2), string.Empty)).GetAwaiter().GetResult();

            Assert.AreEqual(0, effects.SpawnedKeys.Count);
        }

        private static AbilityVfxPresenter CreatePresenter(
            DemoEntityRegistry registry,
            IEffectsFacade effects,
            StubAddressablesFacade addressables = null) =>
            new AbilityVfxPresenter(
                registry,
                effects,
                addressables ?? new StubAddressablesFacade(),
                logger: null,
                new EntityAttachedVfxRegistry());

        private static PresentationVfxIntent CreateIntent(EntityId targetId, string prefabKey) =>
            new PresentationVfxIntent(
                targetId,
                prefabKey,
                VfxPresentationStyle.Default,
                AbilityTargetType.Enemy,
                AbilityDurationType.Instant,
                duration: 0.5f,
                delay: 0f,
                ox: 0f,
                oy: 0f,
                oz: 0f);

        private static EntityView CreateView(EntityId id, Vector3 position, bool isPlayer = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = position;
            EntityView view = go.AddComponent<EntityView>();
            view.Configure(id, isPlayer);
            return view;
        }
    }
}
