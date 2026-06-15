namespace AvantajPrim.Abilities.Execution
{
    /// <summary>Editor / play-mode bridge for menu tools when Zenject container is not directly reachable.</summary>
    public static class AbilityEditorPlayAccess
    {
        public static AbilityActivationLog Log { get; set; }
        public static AbilityActivationReplayService Replay { get; set; }
        public static object HotkeyBindings { get; set; }
        public static object AbilityCatalog { get; set; }
    }
}
