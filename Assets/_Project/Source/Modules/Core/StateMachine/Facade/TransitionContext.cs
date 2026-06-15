namespace StateMachine.Facade
{
    public readonly struct TransitionContext
    {
        public readonly object Payload;

        public TransitionContext(object payload = null)
        {
            Payload = payload;
        }

        public static TransitionContext Empty => default;
    }
}
