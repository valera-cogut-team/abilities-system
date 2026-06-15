using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using Input.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using Input.Facade;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class PlayerInputRouter
    {
        private static readonly int TargetingLayerMask = LayerMask.GetMask(DemoConstants.Layers.Targeting);
        private static readonly int GroundLayerMask = LayerMask.GetMask(DemoConstants.Layers.Ground);

        private readonly TargetingService _targeting;
        private readonly PlayerMovementService _movement;
        private readonly DemoEntityRegistry _registry;
        private readonly EntityStateRegistry _entityStateRegistry;

        private Camera _camera;

        public PlayerInputRouter(
            TargetingService targeting,
            PlayerMovementService movement,
            DemoEntityRegistry registry,
            EntityStateRegistry entityStateRegistry)
        {
            _targeting = targeting;
            _movement = movement;
            _registry = registry;
            _entityStateRegistry = entityStateRegistry;
        }

        public void AttachCamera(Camera camera) => _camera = camera;

        public void FaceTarget(EntityId targetId)
        {
            if (!_registry.TryGetView(targetId, out EntityView view))
                return;

            FacePlayerToward(view.transform.position);
        }

        public bool TryHandleInteract(IInputFacade input)
        {
            if (!PollInteractDown(input, out Vector2 screenPoint))
                return false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                return false;

            Ray ray = _camera.ScreenPointToRay(screenPoint);
            bool multiSelect = IsMultiSelectModifierHeld(input);

            if (TryRaycastEntity(ray, out EntityView entityView))
            {
                if (entityView.EntityId.Equals(_registry.PlayerId))
                {
                    _targeting.ClearSelection();
                    return true;
                }

                if (!entityView.EntityId.Equals(_registry.PlayerId))
                {
                    if (multiSelect)
                    {
                        _targeting.ToggleSelection(entityView.EntityId);
                    }
                    else if (IsSoleSelectedEnemy(entityView.EntityId))
                    {
                        _targeting.ClearSelection();
                        return true;
                    }
                    else
                    {
                        _targeting.SetSingleSelection(entityView.EntityId);
                    }

                    FacePlayerToward(entityView.transform.position);
                    return true;
                }
            }

            if (TryRaycastGround(ray, out Vector3 groundPoint))
            {
                _targeting.ClearSelection();

                if (CanMovePlayer())
                    _movement.MoveTo(groundPoint);

                return true;
            }

            return false;
        }

        private static bool IsMultiSelectModifierHeld(IInputFacade input) =>
            input != null &&
            (input.GetKey((int)KeyCode.LeftShift) || input.GetKey((int)KeyCode.RightShift));

        private bool IsSoleSelectedEnemy(EntityId enemyId) =>
            _targeting.SelectedTargets.Count == 1 &&
            _targeting.IsSelected(enemyId);

        private bool CanMovePlayer()
        {
            if (!_entityStateRegistry.TryGet(_registry.PlayerId, out IEntityStateMachineController controller))
                return false;

            return !controller.IsMovementBlocked &&
                   controller.CanTransition(EntityStatePaths.Locomotion.Walking);
        }

        private bool TryRaycastEntity(Ray ray, out EntityView view)
        {
            view = null;
            int mask = TargetingLayerMask != 0 ? TargetingLayerMask : Physics.DefaultRaycastLayers;

            if (!Physics.Raycast(ray, out RaycastHit hit, DemoConstants.Input.MaxRaycastDistance, mask, QueryTriggerInteraction.Collide))
                return false;

            view = hit.collider.GetComponentInParent<EntityView>();
            return view != null;
        }

        private bool TryRaycastGround(Ray ray, out Vector3 point)
        {
            point = default;
            int mask = GroundLayerMask != 0 ? GroundLayerMask : Physics.DefaultRaycastLayers;

            if (!Physics.Raycast(ray, out RaycastHit hit, DemoConstants.Input.MaxRaycastDistance, mask, QueryTriggerInteraction.Collide))
                return false;

            if (hit.collider.GetComponentInParent<EntityView>() != null)
                return false;

            point = hit.point;
            return true;
        }

        private void FacePlayerToward(Vector3 worldPoint)
        {
            if (!_registry.TryGetView(_registry.PlayerId, out EntityView playerView))
                return;

            if (_entityStateRegistry.TryGet(_registry.PlayerId, out IEntityStateMachineController controller) &&
                controller.IsRotationBlocked)
                return;

            Vector3 direction = worldPoint - playerView.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < DemoConstants.Physics.DirectionEpsilonSqr)
                return;

            playerView.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static bool PollInteractDown(IInputFacade input, out Vector2 screenPoint)
        {
            screenPoint = default;

            if (input.GetButtonDown("Fire1"))
            {
                input.GetPointerPosition(out float x, out float y);
                screenPoint = new Vector2(x, y);
                return true;
            }

            if (input.TouchCount <= 0)
                return false;

            input.GetTouch(0, out float tx, out float ty, out InputTouchPhase phase);
            if (phase != Input.Domain.InputTouchPhase.Began)
                return false;

            screenPoint = new Vector2(tx, ty);
            return true;
        }
    }
}
