using AvantajPrim.Abilities.Facade;
using LifeCycle.Facade;

namespace AvantajPrim.Abilities
{
    public sealed class AbilitiesUpdateHandler : IUpdateHandler
    {
        private readonly IAbilitiesFacade _facade;

        public AbilitiesUpdateHandler(IAbilitiesFacade facade) => _facade = facade;

        public void OnUpdate(float deltaTime) => _facade.OnUpdate(deltaTime);
    }
}
