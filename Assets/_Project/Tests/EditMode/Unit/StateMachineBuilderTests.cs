using System.Collections.Generic;
using NUnit.Framework;
using StateMachine.Application;
using StateMachine.Facade;

namespace AvantajPrim.Tests.EditMode.Unit
{
    public sealed class StateMachineBuilderTests
    {
        private static readonly StatePath Idle = StatePath.Parse("Locomotion.Idle");
        private static readonly StatePath Walking = StatePath.Parse("Locomotion.Walking");
        private static readonly StatePath Dead = StatePath.Parse("Vitality.Dead");
        private static readonly StatePath Combustion = StatePath.Parse("Status.Combustion");
        private static readonly StatePath Freezing = StatePath.Parse("Status.Freezing");

        [Test]
        public void ExclusiveTransition_InvokesEnterExit_InOrder()
        {
            var log = new List<string>();
            IStateMachineDefinition definition = StateMachineBuilder.Create("Entity")
                .ExclusiveRegion("Locomotion")
                .Initial(Idle)
                .State(Idle).OnExit((_, __) => log.Add("exit-idle")).And()
                .State(Walking).OnEnter((_, __) => log.Add("enter-walk")).And()
                .Allow(Idle, Walking)
                .EndRegion()
                .Build();

            using IStateMachineRuntime runtime = definition.CreateRuntime();
            Assert.That(runtime.TryTransition(Walking), Is.True);
            Assert.That(log, Is.EqualTo(new[] { "exit-idle", "enter-walk" }));
            Assert.That(runtime.IsInState(Walking), Is.True);
        }

        [Test]
        public void BlockedTransition_DoesNotInvokeHooks()
        {
            bool entered = false;
            IStateMachineDefinition definition = StateMachineBuilder.Create("Entity")
                .ExclusiveRegion("Locomotion")
                .Initial(Idle)
                .State(Idle).And()
                .State(Walking).OnEnter((_, __) => entered = true).And()
                .Allow(Idle, Walking)
                .EndRegion()
                .Build();

            using IStateMachineRuntime runtime = definition.CreateRuntime();
            Assert.That(runtime.TryTransition(Walking), Is.True);
            entered = false;
            Assert.That(runtime.TryTransition(Idle), Is.False);
            Assert.That(entered, Is.False);
            Assert.That(runtime.IsInState(Walking), Is.True);
        }

        [Test]
        public void GuardBlocksTransition()
        {
            IStateMachineDefinition definition = StateMachineBuilder.Create("Entity")
                .ExclusiveRegion("Vitality")
                .Initial(StatePath.Parse("Vitality.Alive"))
                .State(StatePath.Parse("Vitality.Alive")).And()
                .State(Dead).And()
                .Allow(StatePath.Parse("Vitality.Alive"), Dead)
                .EndRegion()
                .ExclusiveRegion("Locomotion")
                .Initial(Idle)
                .State(Idle).And()
                .State(Walking).And()
                .Allow(Idle, Walking)
                .Allow(Walking, Idle)
                .EndRegion()
                .GlobalGuard(BlockWhenDead)
                .Build();

            using IStateMachineRuntime runtime = definition.CreateRuntime();
            Assert.That(runtime.TryTransition(Walking), Is.True);
            Assert.That(runtime.TryTransition(Dead), Is.True);
            Assert.That(runtime.TryTransition(Idle), Is.False);
        }

        private static bool BlockWhenDead(StatePath from, StatePath to, IStateMachineRuntime runtime, in TransitionContext ctx) =>
            !runtime.IsInState(Dead);

        [Test]
        public void ParallelRegion_AllowsMultipleActiveStates()
        {
            IStateMachineDefinition definition = StateMachineBuilder.Create("Entity")
                .ParallelRegion("Status")
                .State(Combustion).Duration(10f).AndParallel()
                .State(Freezing).Duration(10f).AndParallel()
                .AllowEnter(Combustion)
                .AllowEnter(Freezing)
                .EndRegion()
                .Build();

            using IStateMachineRuntime runtime = definition.CreateRuntime();
            Assert.That(runtime.TryTransition(Combustion), Is.True);
            Assert.That(runtime.TryTransition(Freezing), Is.True);
            Assert.That(runtime.IsInState(Combustion), Is.True);
            Assert.That(runtime.IsInState(Freezing), Is.True);
            Assert.That(runtime.GetActiveStates().Count, Is.EqualTo(2));
        }

        [Test]
        public void ParallelDuration_AutoDeactivatesOnTick()
        {
            bool exited = false;
            IStateMachineDefinition definition = StateMachineBuilder.Create("Entity")
                .ParallelRegion("Status")
                .State(Combustion)
                    .Duration(0.5f)
                    .OnExit((_, __) => exited = true)
                    .AndParallel()
                .AllowEnter(Combustion)
                .EndRegion()
                .Build();

            using IStateMachineRuntime runtime = definition.CreateRuntime();
            runtime.TryTransition(Combustion);
            runtime.Tick(0.6f);
            Assert.That(exited, Is.True);
            Assert.That(runtime.IsInState(Combustion), Is.False);
        }
    }
}
