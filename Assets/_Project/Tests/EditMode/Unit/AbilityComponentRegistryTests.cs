using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.Abilities.Execution.Executors;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityComponentRegistryTests
    {
        [Test]
        public void TryResolve_ReturnsRegisteredExecutor_ForComponentDataType()
        {
            var registry = new AbilityComponentRegistry();
            var executor = new DamageComponentExecutor();
            registry.Register(executor);

            var data = new DamageComponentData();
            bool resolved = registry.TryResolve(data, out IAbilityComponentExecutor found);

            Assert.IsTrue(resolved);
            Assert.AreSame(executor, found);
        }

        [Test]
        public void TryResolve_ReturnsFalse_WhenDataIsNull()
        {
            var registry = new AbilityComponentRegistry();
            registry.Register(new DamageComponentExecutor());

            bool resolved = registry.TryResolve(null, out IAbilityComponentExecutor found);

            Assert.IsFalse(resolved);
            Assert.IsNull(found);
        }

        [Test]
        public void TryResolve_ReturnsFalse_WhenExecutorNotRegistered()
        {
            var registry = new AbilityComponentRegistry();
            var data = new DamageComponentData();

            bool resolved = registry.TryResolve(data, out IAbilityComponentExecutor found);

            Assert.IsFalse(resolved);
            Assert.IsNull(found);
        }
    }
}
