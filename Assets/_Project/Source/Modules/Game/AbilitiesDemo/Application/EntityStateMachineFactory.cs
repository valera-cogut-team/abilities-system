using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using StateMachine.Application;
using StateMachine.Facade;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public static class EntityStateMachineFactory
    {
        public static IStateMachineDefinition CreateDefinition(
            bool isPlayer,
            EntityAnimationPresenter presenter,
            IEntityStateMachineController controller)
        {
            StateMachineBuilder builder = StateMachineBuilder.Create(DemoConstants.Fsm.MachineName)
                .ExclusiveRegion(DemoConstants.Fsm.VitalityRegion)
                .Initial(EntityStatePaths.Vitality.Alive)
                .State(EntityStatePaths.Vitality.Alive).And()
                .State(EntityStatePaths.Vitality.Dead)
                    .OnEnter((_, __) =>
                    {
                        presenter?.PlayDead();
                        controller?.ReleasePresentationEffects();
                    })
                    .And()
                .Allow(EntityStatePaths.Vitality.Alive, EntityStatePaths.Vitality.Dead)
                .EndRegion();

            builder = AddActionRegion(builder, presenter, controller);
            builder = AddInputRegion(builder);
            builder = AddLocomotionRegion(builder, isPlayer, presenter, controller);
            builder = AddStatusRegion(builder, controller);

            builder = builder
                .GlobalGuard(EntityStateGuards.NotDead)
                .GlobalGuard(EntityStateGuards.NotMovementLocked);

            if (!isPlayer)
            {
                builder = builder
                    .ExclusiveRegion(DemoConstants.Fsm.AiRegion)
                    .Initial(EntityStatePaths.Ai.Idle)
                    .State(EntityStatePaths.Ai.Idle).And()
                    .EndRegion();
            }

            return builder.Build();
        }

        private static StateMachineBuilder AddActionRegion(
            StateMachineBuilder builder,
            EntityAnimationPresenter presenter,
            IEntityStateMachineController controller)
        {
            return builder.ExclusiveRegion(DemoConstants.Fsm.ActionRegion)
                .Initial(EntityStatePaths.Action.None)
                .State(EntityStatePaths.Action.None)
                    .OnEnter((_, __) => presenter?.ReturnToIdle())
                    .And()
                .State(EntityStatePaths.Action.HitReact)
                    .OnEnter((_, __) =>
                    {
                        presenter?.PlayHitReact();
                        controller?.ScheduleHitReactExit(DemoConstants.Combat.HitReactExitDelaySeconds);
                    })
                    .And()
                .Allow(EntityStatePaths.Action.None, EntityStatePaths.Action.HitReact)
                .Allow(EntityStatePaths.Action.HitReact, EntityStatePaths.Action.None)
                .Allow(EntityStatePaths.Action.None, EntityStatePaths.Action.CastingWildcard)
                .Allow(EntityStatePaths.Action.CastingWildcard, EntityStatePaths.Action.CastingWildcard)
                .AllowTo(EntityStatePaths.Action.None)
                .EndRegion();
        }

        private static StateMachineBuilder AddInputRegion(StateMachineBuilder builder)
        {
            return builder.ParallelRegion(DemoConstants.Fsm.InputRegion)
                .State(EntityStatePaths.Input.Movement).AndParallel()
                .State(EntityStatePaths.Input.Rotation).AndParallel()
                .AllowEnter(EntityStatePaths.Input.Movement)
                .AllowEnter(EntityStatePaths.Input.Rotation)
                .EndRegion();
        }

        private static StateMachineBuilder AddLocomotionRegion(
            StateMachineBuilder builder,
            bool isPlayer,
            EntityAnimationPresenter presenter,
            IEntityStateMachineController controller)
        {
            StateMachineBuilder.ExclusiveRegionBuilder locomotion = builder.ExclusiveRegion(DemoConstants.Fsm.LocomotionRegion)
                .Initial(EntityStatePaths.Locomotion.Idle)
                .State(EntityStatePaths.Locomotion.Idle)
                    .OnEnter((_, __) => presenter?.SetWalking(false))
                    .And()
                .State(EntityStatePaths.Locomotion.Walking)
                    .OnEnter((_, __) => presenter?.SetWalking(true))
                    .OnExit((_, __) => presenter?.SetWalking(false))
                    .And()
                .State(EntityStatePaths.Locomotion.Displaced)
                    .OnEnter((runtime, ctx) =>
                    {
                        if (presenter == null || controller == null)
                            return;

                        if (ctx.Payload is DisplacementTransitionPayload payload)
                        {
                            var offset = new UnityEngine.Vector3(payload.OffsetX, payload.OffsetY, payload.OffsetZ);
                            presenter.ApplyDisplacement(offset, payload.Duration, () =>
                            {
                                controller.CompleteCastEffect(payload.CastLifecycleId);
                                controller.TryTransition(EntityStatePaths.Locomotion.Idle);
                            });
                        }
                    })
                    .And();

            if (isPlayer)
            {
                locomotion = locomotion
                    .Allow(EntityStatePaths.Locomotion.Idle, EntityStatePaths.Locomotion.Walking)
                    .Allow(EntityStatePaths.Locomotion.Walking, EntityStatePaths.Locomotion.Idle)
                    .Allow(EntityStatePaths.Locomotion.Idle, EntityStatePaths.Locomotion.Displaced)
                    .Allow(EntityStatePaths.Locomotion.Walking, EntityStatePaths.Locomotion.Displaced)
                    .Allow(EntityStatePaths.Locomotion.Displaced, EntityStatePaths.Locomotion.Idle);
            }

            return locomotion.EndRegion();
        }

        private static StateMachineBuilder AddStatusRegion(
            StateMachineBuilder builder,
            IEntityStateMachineController controller)
        {
            StateMachineBuilder.ParallelRegionBuilder status = builder.ParallelRegion(DemoConstants.Fsm.StatusRegion);

            foreach (StatePath path in new[]
                     {
                         EntityStatePaths.Status.Combustion,
                         EntityStatePaths.Status.Freezing,
                         EntityStatePaths.Status.Healing,
                         EntityStatePaths.Status.Bleeding
                     })
            {
                status = status
                    .State(path)
                    .OnEnter((_, ctx) => controller?.OnStatusEnter(path, ctx))
                    .OnExit((_, __) => controller?.OnStatusExit(path))
                    .OnUpdate((_, dt) => controller?.TickStatus(path, dt))
                    .AndParallel();
            }

            return status
                .AllowEnter(EntityStatePaths.Status.Combustion)
                .AllowEnter(EntityStatePaths.Status.Freezing)
                .AllowEnter(EntityStatePaths.Status.Healing)
                .AllowEnter(EntityStatePaths.Status.Bleeding)
                .EndRegion();
        }
    }
}
