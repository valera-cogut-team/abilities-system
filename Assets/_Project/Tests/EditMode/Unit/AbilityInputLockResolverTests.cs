using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.Tests.Shared;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityInputLockResolverTests
    {
        [Test]
        public void BlocksMovement_ReturnsTrue_WhenOnStartLockBlocksMovementOnly()
        {
            AbilityDefinition definition = CreateDefinition(new LockInputComponentData
            {
                BlockMovement = true,
                BlockRotation = false
            });

            Assert.IsTrue(AbilityInputLockResolver.BlocksMovement(definition));
            Assert.IsFalse(AbilityInputLockResolver.BlocksRotation(definition));
        }

        [Test]
        public void BlocksRotation_ReturnsTrue_WhenOnStartLockBlocksRotationOnly()
        {
            AbilityDefinition definition = CreateDefinition(new LockInputComponentData
            {
                BlockMovement = false,
                BlockRotation = true
            });

            Assert.IsFalse(AbilityInputLockResolver.BlocksMovement(definition));
            Assert.IsTrue(AbilityInputLockResolver.BlocksRotation(definition));
        }

        [Test]
        public void TryGetCasterOnStartLock_ReturnsFalse_WhenNoLockComponent()
        {
            AbilityDefinition definition = CreateDefinition(TestAddressableRefs.CreateSoundComponent());

            Assert.IsFalse(AbilityInputLockResolver.TryGetCasterOnStartLock(
                definition,
                out bool blockMovement,
                out bool blockRotation));
            Assert.IsFalse(blockMovement);
            Assert.IsFalse(blockRotation);
        }

        private static AbilityDefinition CreateDefinition(IAbilityComponentData component) =>
            new AbilityDefinition(
                new AbilityId("test"),
                new List<IAbilityComponentData> { component });
    }
}
