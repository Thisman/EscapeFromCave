using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameEventBusService : MonoBehaviour
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    private readonly object _lock = new();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            var eventType = typeof(TEvent);

            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers.Add(eventType, handlers);
            }

            handlers.Add(handler);
        }

        return new Subscription<TEvent>(this, handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) return;

        lock (_lock)
        {
            var eventType = typeof(TEvent);

            if (!_subscribers.TryGetValue(eventType, out var handlers))
                return;

            handlers.Remove(handler);

            if (handlers.Count == 0)
                _subscribers.Remove(eventType);
        }
    }

    public void Publish<TEvent>(TEvent evt)
    {
        List<Delegate> snapshot;

        lock (_lock)
        {
            var eventType = typeof(TEvent);

            if (!_subscribers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                return;

            snapshot = new List<Delegate>(handlers);
        }

        foreach (var del in snapshot)
        {
            if (del is Action<TEvent> action)
            {
                action.Invoke(evt);
            }
        }
    }

    public bool HasSubscribers<TEvent>()
    {
        lock (_lock)
        {
            var eventType = typeof(TEvent);
            return _subscribers.TryGetValue(eventType, out var handlers) && handlers.Count > 0;
        }
    }

    private sealed class Subscription<TEvent> : IDisposable
    {
        private readonly GameEventBusService _bus;
        private Action<TEvent> _handler;

        public Subscription(GameEventBusService bus, Action<TEvent> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_handler != null)
            {
                _bus.Unsubscribe(_handler);
                _handler = null;
            }
        }
    }
}
