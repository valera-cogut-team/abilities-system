using System;

namespace Core.Observable
{
    /// <summary>
    /// Lightweight in-process domain event bus for decoupled communication between modules.
    /// Follows the Domain Events pattern from DDD — events represent something that happened
    /// in the domain that other parts of the system may react to.
    /// </summary>
    public interface IDomainEventBus
    {
        void Publish<TEvent>(TEvent evt);
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        void UnsubscribeAll();
    }
}