using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Architecture
{
    [TestFixture]
    public sealed class DomainAsmdefTests
    {
        private const string RelativePath = "_Project/Source/Modules/Core/Abilities/Domain/Abilities.Domain.asmdef";

        [Test]
        public void AbilitiesDomain_HasNoEngineReferences()
        {
            string path = Path.Combine(Application.dataPath, RelativePath);
            Assert.IsTrue(File.Exists(path), $"Missing asmdef at {path}");

            string json = File.ReadAllText(path);
            Assert.IsTrue(Regex.IsMatch(json, @"""noEngineReferences""\s*:\s*true"),
                "Abilities.Domain must set noEngineReferences to true.");
        }

        [Test]
        public void AbilitiesDomain_ReferencesOnlyStateMachine()
        {
            string path = Path.Combine(Application.dataPath, RelativePath);
            string json = File.ReadAllText(path);

            Assert.IsTrue(Regex.IsMatch(json, @"""references""\s*:\s*\[\s*""StateMachine""\s*\]"),
                "Abilities.Domain may reference only StateMachine for entity FSM ports.");
        }

        [Test]
        public void AbilitiesDomain_DoesNotReferenceUnityAssemblies()
        {
            string path = Path.Combine(Application.dataPath, RelativePath);
            string json = File.ReadAllText(path);

            Assert.IsFalse(Regex.IsMatch(json, @"UnityEngine|UnityEditor|Unity\."),
                "Abilities.Domain asmdef must not reference Unity assemblies.");
        }
    }

    [TestFixture]
    public sealed class ExecutionAsmdefTests
    {
        private const string RelativePath = "_Project/Source/Modules/Core/Abilities/Execution/Abilities.Execution.asmdef";

        [Test]
        public void AbilitiesExecution_HasNoEngineReferences()
        {
            string path = Path.Combine(Application.dataPath, RelativePath);
            Assert.IsTrue(File.Exists(path), $"Missing asmdef at {path}");

            string json = File.ReadAllText(path);
            Assert.IsTrue(Regex.IsMatch(json, @"""noEngineReferences""\s*:\s*true"),
                "Abilities.Execution must set noEngineReferences to true.");
        }

        [Test]
        public void AbilitiesExecution_DoesNotReferenceUnityAssemblies()
        {
            string path = Path.Combine(Application.dataPath, RelativePath);
            string json = File.ReadAllText(path);

            Assert.IsFalse(Regex.IsMatch(json, @"UnityEngine|UnityEditor|Unity\.Addressables"),
                "Abilities.Execution asmdef must not reference Unity assemblies.");
        }
    }
}
