using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Application;
using NUnit.Framework;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityHotkeyBindingServiceTests
    {
        private AbilityCatalog _catalog;
        private AbilityHotkeyBindingService _hotkeys;

        [SetUp]
        public void SetUp()
        {
            _catalog = new AbilityCatalog();
            _catalog.Register(new AbilityDefinition(
                new AbilityId("dash"),
                new List<IAbilityComponentData>(),
                hotkeySlot: 1));
            _catalog.Register(new AbilityDefinition(
                new AbilityId("firewall"),
                new List<IAbilityComponentData>(),
                hotkeySlot: 2,
                hotkeyKeyCode: (int)KeyCode.F));
            _hotkeys = new AbilityHotkeyBindingService(_catalog);
        }

        [Test]
        public void GetEffectiveKeyCode_UsesSlotWhenNoExplicitKey()
        {
            _catalog.TryGet(new AbilityId("dash"), out AbilityDefinition dash);

            Assert.AreEqual((int)KeyCode.Alpha1, _hotkeys.GetEffectiveKeyCode(dash));
        }

        [Test]
        public void GetEffectiveKeyCode_UsesExplicitKeyFromDefinition()
        {
            _catalog.TryGet(new AbilityId("firewall"), out AbilityDefinition firewall);

            Assert.AreEqual((int)KeyCode.F, _hotkeys.GetEffectiveKeyCode(firewall));
        }

        [Test]
        public void GetEffectiveKeyCode_RuntimeOverrideTakesPrecedence()
        {
            _catalog.TryGet(new AbilityId("dash"), out AbilityDefinition dash);
            _hotkeys.SetRuntimeOverride("dash", (int)KeyCode.Q);

            Assert.AreEqual((int)KeyCode.Q, _hotkeys.GetEffectiveKeyCode(dash));
        }

        [Test]
        public void ClearRuntimeOverride_RestoresDefinitionKey()
        {
            _catalog.TryGet(new AbilityId("dash"), out AbilityDefinition dash);
            _hotkeys.SetRuntimeOverride("dash", (int)KeyCode.Q);
            _hotkeys.ClearRuntimeOverride("dash");

            Assert.AreEqual((int)KeyCode.Alpha1, _hotkeys.GetEffectiveKeyCode(dash));
        }
    }
}
