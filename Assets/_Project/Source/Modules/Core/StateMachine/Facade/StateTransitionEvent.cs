namespace StateMachine.Facade
{
    public readonly struct StateTransitionEvent
    {
        public readonly string Region;
        public readonly StatePath From;
        public readonly StatePath To;
        public readonly TransitionContext Context;
        public readonly bool IsParallelEnter;
        public readonly bool IsParallelExit;

        public StateTransitionEvent(
            string region,
            StatePath from,
            StatePath to,
            TransitionContext context,
            bool isParallelEnter = false,
            bool isParallelExit = false)
        {
            Region = region;
            From = from;
            To = to;
            Context = context;
            IsParallelEnter = isParallelEnter;
            IsParallelExit = isParallelExit;
        }
    }
}
