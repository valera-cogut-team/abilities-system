using System;
using System.Collections.Generic;
using LifeCycle.Facade;

namespace AvantajPrim.AbilitiesDemo.Application
{
    /// <summary>
    /// Manages registration/unregistration of update handlers with the lifecycle module.
    /// Extracted from AbilitiesDemoModule to reduce method complexity and centralize handler lifecycle.
    /// </summary>
    public sealed class AbilitiesDemoWorldBinder
    {
        private readonly ILifeCycleFacade _lifeCycle;
        private readonly List<IUpdateHandler> _updateHandlers = new List<IUpdateHandler>(8);
        private readonly List<ILateUpdateHandler> _lateUpdateHandlers = new List<ILateUpdateHandler>(2);

        public AbilitiesDemoWorldBinder(ILifeCycleFacade lifeCycle)
        {
            _lifeCycle = lifeCycle ?? throw new ArgumentNullException(nameof(lifeCycle));
        }

        public void RegisterUpdate(IUpdateHandler handler)
        {
            if (handler == null) return;
            _lifeCycle.RegisterUpdateHandler(handler);
            _updateHandlers.Add(handler);
        }

        public void RegisterLateUpdate(ILateUpdateHandler handler)
        {
            if (handler == null) return;
            _lifeCycle.RegisterLateUpdateHandler(handler);
            _lateUpdateHandlers.Add(handler);
        }

        public void UnregisterAll()
        {
            foreach (IUpdateHandler handler in _updateHandlers)
                _lifeCycle.UnregisterUpdateHandler(handler);
            _updateHandlers.Clear();

            foreach (ILateUpdateHandler handler in _lateUpdateHandlers)
                _lifeCycle.UnregisterLateUpdateHandler(handler);
            _lateUpdateHandlers.Clear();
        }
    }
}