using System.Collections;
using AvantajPrim.Abilities;
using AvantajPrim.Abilities.Facade;
using Core;
using LifeCycle;
using Logger;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace AvantajPrim.Tests.PlayMode
{
    public sealed class AbilitiesModuleSmokeTests
    {
        [UnityTest]
        public IEnumerator AbilitiesModule_Initializes_WithLoggerAndLifeCycle()
        {
            var sceneGo = new GameObject("AbilitiesModuleSmokeTest");
            var container = new DiContainer();

            var moduleManager = new ModuleManager(container);

            var logger = new LoggerModule();
            logger.Initialize(moduleManager.Context);
            logger.Enable();
            moduleManager.RegisterModule(logger);

            var lifeCycle = new LifeCycleModule();
            lifeCycle.Initialize(moduleManager.Context);
            lifeCycle.Enable();
            moduleManager.RegisterModule(lifeCycle);

            var abilities = new AbilitiesModule();
            abilities.Initialize(moduleManager.Context);
            moduleManager.RegisterModule(abilities);

            IAbilitiesFacade facade = moduleManager.Context.Container.Resolve<IAbilitiesFacade>();
            Assert.IsNotNull(facade);

            abilities.Enable();
            Assert.IsTrue(abilities.IsEnabled);

            abilities.Shutdown();
            Object.Destroy(sceneGo);
            yield return null;
        }
    }
}
