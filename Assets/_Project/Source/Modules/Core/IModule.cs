namespace Core
{
    /// <summary>Base interface for all modular game features (Core, UI, gameplay).</summary>
    public interface IModule
    {
        string Name { get; }
        string Version { get; }
        string[] Dependencies { get; }
        bool IsEnabled { get; }
        void Initialize(IModuleContext context);
        void Enable();
        void Disable();
        void Shutdown();
    }
}
