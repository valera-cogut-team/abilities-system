using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using LifeCycle.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class PlayerMovementService : IUpdateHandler
    {
        public const float DefaultMoveSpeed = DemoConstants.Movement.DefaultMoveSpeed;
        public const float ArrivalThreshold = DemoConstants.Movement.ArrivalThreshold;

        private readonly DemoEntityRegistry _registry;
        private readonly EntityStateRegistry _entityStateRegistry;

        private EntityView _activeView;
        private IEntityStateMachineController _activeController;
        private Vector3 _flatTarget;
        private float _speed;

        public PlayerMovementService(
            DemoEntityRegistry registry,
            EntityStateRegistry entityStateRegistry)
        {
            _registry = registry;
            _entityStateRegistry = entityStateRegistry;
        }

        public bool IsMoving { get; private set; }

        public void CancelCurrent()
        {
            IsMoving = false;
            _activeView = null;
            _activeController = null;

            if (!_entityStateRegistry.TryGet(_registry.PlayerId, out IEntityStateMachineController controller))
                return;

            controller.TryTransition(EntityStatePaths.Locomotion.Idle);
        }

        public void MoveTo(Vector3 worldPoint)
        {
            if (IsPlayerMovementBlocked())
                return;

            if (_registry.PlayerId == default ||
                !_registry.TryGetView(_registry.PlayerId, out EntityView view) ||
                !_entityStateRegistry.TryGet(_registry.PlayerId, out IEntityStateMachineController controller))
                return;

            if (!controller.CanTransition(EntityStatePaths.Locomotion.Walking))
                return;

            CancelCurrent();
            controller.TryTransition(EntityStatePaths.Locomotion.Walking);

            _activeView = view;
            _activeController = controller;
            _flatTarget = new Vector3(worldPoint.x, view.transform.position.y, worldPoint.z);
            _speed = DemoConstants.Movement.DefaultMoveSpeed;
            IsMoving = true;
        }

        public void OnUpdate(float deltaTime)
        {
            if (IsPlayerMovementBlocked())
            {
                if (IsMoving)
                    StopMoving();
                return;
            }

            if (!IsMoving || _activeView == null || _activeController == null)
                return;

            if (!_activeController.IsInState(EntityStatePaths.Locomotion.Walking))
            {
                StopMoving();
                return;
            }

            Vector3 pos = _activeView.transform.position;
            Vector3 to = _flatTarget - pos;
            to.y = 0f;

            if (to.sqrMagnitude <= DemoConstants.Movement.ArrivalThreshold * DemoConstants.Movement.ArrivalThreshold)
            {
                StopMoving();
                return;
            }

            Vector3 step = to.normalized * (_speed * deltaTime);
            if (step.sqrMagnitude > to.sqrMagnitude)
                step = to;

            _activeView.transform.position = pos + step;

            if (step.sqrMagnitude > DemoConstants.Physics.DirectionEpsilonSqr && !_activeController.IsRotationBlocked)
            {
                var look = Quaternion.LookRotation(step.normalized, Vector3.up);
                _activeView.transform.rotation = Quaternion.Slerp(
                    _activeView.transform.rotation,
                    look,
                    deltaTime * DemoConstants.Movement.RotationSlerpSpeed);
            }
        }

        private bool IsPlayerMovementBlocked()
        {
            if (_registry.PlayerId == default ||
                !_entityStateRegistry.TryGet(_registry.PlayerId, out IEntityStateMachineController controller))
                return false;

            return controller.IsMovementBlocked;
        }

        private void StopMoving()
        {
            IsMoving = false;
            _activeController?.TryTransition(EntityStatePaths.Locomotion.Idle);
            _activeView = null;
            _activeController = null;
        }
    }
}
