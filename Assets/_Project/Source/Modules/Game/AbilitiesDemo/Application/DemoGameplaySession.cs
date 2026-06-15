namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class DemoGameplaySession
    {
        private readonly TargetingService _targeting;
        private readonly PlayerMovementService _movement;

        public DemoGameplaySession(TargetingService targeting, PlayerMovementService movement)
        {
            _targeting = targeting;
            _movement = movement;
        }

        public bool IsActive { get; private set; }
        public bool IsWorldVisible { get; private set; }
        public float GroundSurfaceY { get; private set; }

        public void SetGroundSurfaceY(float groundSurfaceY) => GroundSurfaceY = groundSurfaceY;

        public void SetWorldVisible(bool visible)
        {
            IsWorldVisible = visible;
        }

        public void SetActive(bool active)
        {
            if (IsActive == active)
                return;

            IsActive = active;

            if (active)
                return;

            _targeting.ClearSelection();
            _movement.CancelCurrent();
        }
    }
}
