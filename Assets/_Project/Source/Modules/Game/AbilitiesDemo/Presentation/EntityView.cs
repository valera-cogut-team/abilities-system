using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using AvantajPrim.AbilitiesDemo.Presentation;
using UnityEngine;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.Abilities.Domain;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    /// <summary>Transform/collider/VFX spawn point; delegates animation to <see cref="EntityAnimationPresenter"/>.</summary>
    public sealed class EntityView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _vfxSpawnPoint;

        private AbilityEntityModel _entityModel;
        private IEntityStateMachineController _stateMachine;

        public EntityId EntityId { get; private set; }
        public string DisplayName { get; private set; }
        public IAbilityEntity EntityModel => _entityModel;
        public EntityHealthBar HealthBar { get; private set; }
        public Animator Animator => _animator != null ? _animator : GetComponentInChildren<Animator>();
        public IEntityStateMachineController StateMachine => _stateMachine;

        public void Configure(EntityId id, bool isPlayer, string displayName = null)
        {
            EntityId = id;
            DisplayName = displayName ?? (isPlayer
                ? DemoConstants.Entity.PlayerDisplayName
                : string.Format(DemoConstants.Entity.EnemyDisplayNameFormat, id.Value));
            _entityModel = new AbilityEntityModel(id, isPlayer);

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_vfxSpawnPoint == null)
                _vfxSpawnPoint = transform;
        }

        internal void BindStateMachine(IEntityStateMachineController controller)
        {
            _stateMachine = controller;
        }

        public void AttachHealthBar(EntityHealthBar bar)
        {
            HealthBar = bar;
        }

        public void ReturnToIdle()
        {
            _stateMachine?.TryTransition(Domain.EntityStatePaths.Action.None);
            _stateMachine?.TryTransition(Domain.EntityStatePaths.Locomotion.Idle);
        }

        public float GetBodyCenterLocalY()
        {
            if (TryGetComponent<CapsuleCollider>(out CapsuleCollider capsule))
                return capsule.center.y;

            return DemoConstants.Entity.DefaultBodyCenterLocalY;
        }

        public Vector3 GetVfxWorldPosition(float offsetX, float offsetY, float offsetZ)
        {
            Vector3 origin = _vfxSpawnPoint != null ? _vfxSpawnPoint.position : transform.position;
            return origin + new Vector3(offsetX, offsetY, offsetZ);
        }

        public Transform GetFloatingNumbersAnchor() => _vfxSpawnPoint != null ? _vfxSpawnPoint : transform;
    }
}
