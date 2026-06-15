using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilitiesDemoBootstrapTests
    {
        [Test]
        public void EnemyCount_IsThree()
        {
            Assert.AreEqual(3, DemoConstants.Entity.EnemyCount);
        }

        [Test]
        public void DemoEntityRegistry_TracksPlayerAndThreeEnemies()
        {
            var registry = new DemoEntityRegistry();
            registry.SetPlayer(new AvantajPrim.Abilities.Domain.EntityId(1), null);
            registry.AddEnemy(new AvantajPrim.Abilities.Domain.EntityId(2), null);
            registry.AddEnemy(new AvantajPrim.Abilities.Domain.EntityId(3), null);
            registry.AddEnemy(new AvantajPrim.Abilities.Domain.EntityId(4), null);

            Assert.AreEqual(new AvantajPrim.Abilities.Domain.EntityId(1), registry.PlayerId);
            Assert.AreEqual(3, registry.EnemyIds.Count);
        }
    }
}
