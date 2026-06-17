using System;
using System.Collections.Generic;

namespace Core.Observable
{
    /// <summary>
    /// Thread-safe in-process domain event bus implementation.
    /// Uses SafeStream internally for allocation-free event distribution.
    /// </summary>
    public sealed class DomainEventBus : IDomainEventBus
    {
        private readonly Dictionary<Type, object> _streams = new Dictionary<Type, object>();
        private readonly object _lock = new object();
        private bool _disposed;

        public void Publish<TEvent>(TEvent evt)
        {
            SafeStream<TEvent> stream;
            lock (_lock)
            {
                if (_disposed) return;
                if (!_streams.TryGetValue(typeof(TEvent), out object existing)) return;
                stream = (SafeStream<TEvent>)existing;
            }

            stream.Publish(evt);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var observer = new DelegateObserver<TEvent>(handler);
            SafeStream<TEvent> stream;

            lock (_lock)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(DomainEventBus));

                if (!_streams.TryGetValue(typeof(TEvent), out object existing))
                {
                    stream = new SafeStream<TEvent>();
                    _streams[typeof(TEvent)] = stream;
                }
                else
                {
                    stream = (SafeStream<TEvent>)existing;
                }
            }

            return stream.Subscribe(observer);
        }

        public void UnsubscribeAll()
        {
            lock (_lock)
            {
                foreach (object stream in _streams.Values)
                {
                    if (stream is IDisposable disposable)
                        disposable.Dispose();
                }
                _streams.Clear();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                UnsubscribeAll();
            }
        }

        private sealed class DelegateObserver<T> : IObserver<T>
        {
            private readonly Action<T> _onNext;

            public DelegateObserver(Action<T> onNext) => _onNext = onNext;
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(T value) => _onNext(value);
        }
    }
}