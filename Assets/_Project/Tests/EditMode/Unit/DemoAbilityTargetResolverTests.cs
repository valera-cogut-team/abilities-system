using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class DemoAbilityTargetResolverTests
    {
        private DemoAbilityTargetResolver _resolver;
        private DemoEntityRegistry _registry;
        private TargetingService _targeting;

        [SetUp]
        public void SetUp()
        {
            _registry = new DemoEntityRegistry();
            _targeting = new TargetingService();
            _resolver = new DemoAbilityTargetResolver(_registry, _targeting);
            RegisterPlayerAndEnemies();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (EntityView view in Object.FindObjectsByType<EntityView>(FindObjectsSortMode.None))
                Object.DestroyImmediate(view.gameObject);
        }

        [Test]
        public void ResolveTargets_EnemyAbility_ReturnsEmpty_WhenNoSelection()
        {
            _targeting.ClearSelection();

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId(DemoConstants.AbilityIds.Firewall),
                CreateDefinition(DemoConstants.AbilityIds.Firewall, AbilityTargetType.Enemy));

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void ResolveTargets_EnemyAbility_ReturnsEmpty_AfterSelectionWasCleared()
        {
            _targeting.SetSingleSelection(new EntityId(3));
            _targeting.ClearSelection();

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId(DemoConstants.AbilityIds.Dash),
                CreateDefinition(DemoConstants.AbilityIds.Dash, AbilityTargetType.Enemy));

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void ResolveTargets_EnemyAbility_ReturnsOnlySelectedEnemies()
        {
            _targeting.SetSingleSelection(new EntityId(3));
            _targeting.ToggleSelection(new EntityId(4));

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId(DemoConstants.AbilityIds.Firewall),
                CreateDefinition(DemoConstants.AbilityIds.Firewall, AbilityTargetType.Enemy));

            Assert.AreEqual(2, targets.Count);
            Assert.Contains(new EntityId(3), targets);
            Assert.Contains(new EntityId(4), targets);
        }

        [Test]
        public void ResolveTargets_EnemyAbility_IgnoresPlayerInSelection()
        {
            _targeting.SetSingleSelection(_registry.PlayerId);

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId(DemoConstants.AbilityIds.Firewall),
                CreateDefinition(DemoConstants.AbilityIds.Firewall, AbilityTargetType.Enemy));

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void ResolveTargets_PlayerTarget_ReturnsCaster_WhenNoSelection()
        {
            _targeting.ClearSelection();

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId("any_player_target"),
                CreateDefinition("any_player_target", AbilityTargetType.Player));

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(_registry.PlayerId, targets[0]);
        }

        [Test]
        public void ResolveTargets_PlayerTarget_ReturnsCaster_IgnoringEnemySelection()
        {
            _targeting.SetSingleSelection(new EntityId(2));
            _targeting.ToggleSelection(new EntityId(4));

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId(DemoConstants.AbilityIds.Healing),
                CreateDefinition(DemoConstants.AbilityIds.Healing, AbilityTargetType.Player));

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(_registry.PlayerId, targets[0]);
        }

        [Test]
        public void ResolveTargets_EnemyTarget_RequiresSelection_EvenForHealingId()
        {
            _targeting.ClearSelection();

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId(DemoConstants.AbilityIds.Healing),
                CreateDefinition(DemoConstants.AbilityIds.Healing, AbilityTargetType.Enemy));

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void ResolveTargets_EnemyTarget_ReturnsSelectedEnemies_ForAnyAbilityId()
        {
            _targeting.SetSingleSelection(new EntityId(3));

            List<EntityId> targets = _resolver.ResolveTargets(
                new AbilityId(DemoConstants.AbilityIds.Healing),
                CreateDefinition(DemoConstants.AbilityIds.Healing, AbilityTargetType.Enemy));

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(new EntityId(3), targets[0]);
        }

        private static AbilityDefinition CreateDefinition(string abilityId, AbilityTargetType targetType) =>
            new AbilityDefinition(
                new AbilityId(abilityId),
                new List<IAbilityComponentData>(),
                targetType: targetType);

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
    }
}
