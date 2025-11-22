using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class GameEventBusService
{
    private readonly Dictionary<Type, List<Delegate>> _syncHandlers = new();
    private readonly Dictionary<Type, List<Delegate>> _asyncHandlers = new();
    private readonly object _lock = new();

    // ===================== Sync =====================
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            var type = typeof(TEvent);
            if (!_syncHandlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _syncHandlers.Add(type, list);
            }

            list.Add(handler);
        }

        return new SyncSubscription<TEvent>(this, handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) return;

        lock (_lock)
        {
            var type = typeof(TEvent);
            if (!_syncHandlers.TryGetValue(type, out var list))
                return;

            list.Remove(handler);
            if (list.Count == 0)
                _syncHandlers.Remove(type);
        }
    }

    // ===================== Async =====================
    public IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            var type = typeof(TEvent);
            if (!_asyncHandlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _asyncHandlers.Add(type, list);
            }

            list.Add(handler);
        }

        return new AsyncSubscription<TEvent>(this, handler);
    }

    public void UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler)
    {
        if (handler == null) return;

        lock (_lock)
        {
            var type = typeof(TEvent);
            if (!_asyncHandlers.TryGetValue(type, out var list))
                return;

            list.Remove(handler);
            if (list.Count == 0)
                _asyncHandlers.Remove(type);
        }
    }

    // ===================== Public sync =====================
    public void Publish<TEvent>(TEvent evt)
    {
        List<Delegate> syncSnapshot = null;
        List<Delegate> asyncSnapshot = null;

        lock (_lock)
        {
            var type = typeof(TEvent);

            if (_syncHandlers.TryGetValue(type, out var syncList) && syncList.Count > 0)
                syncSnapshot = new List<Delegate>(syncList);

            if (_asyncHandlers.TryGetValue(type, out var asyncList) && asyncList.Count > 0)
                asyncSnapshot = new List<Delegate>(asyncList);
        }

        // Сначала синхронные
        if (syncSnapshot != null)
        {
            foreach (var del in syncSnapshot)
            {
                if (del is Action<TEvent> action)
                {
                    action.Invoke(evt);
                }
            }
        }

        // Async — fire & forget (ошибки желательно логировать)
        if (asyncSnapshot != null)
        {
            foreach (var del in asyncSnapshot)
            {
                if (del is Func<TEvent, Task> func)
                {
                    // Игнорируем Task, но запускаем
                    _ = SafeInvokeAsync(func, evt);
                }
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent evt)
    {
        List<Delegate> syncSnapshot = null;
        List<Delegate> asyncSnapshot = null;

        lock (_lock)
        {
            var type = typeof(TEvent);

            if (_syncHandlers.TryGetValue(type, out var syncList) && syncList.Count > 0)
                syncSnapshot = new List<Delegate>(syncList);

            if (_asyncHandlers.TryGetValue(type, out var asyncList) && asyncList.Count > 0)
                asyncSnapshot = new List<Delegate>(asyncList);
        }

        // Сначала синхронные — сразу, в текущем потоке
        if (syncSnapshot != null)
        {
            foreach (var del in syncSnapshot)
            {
                if (del is Action<TEvent> action)
                {
                    action.Invoke(evt);
                }
            }
        }

        // Затем асинхронные — собираем и ждём
        if (asyncSnapshot != null)
        {
            var tasks = new List<Task>(asyncSnapshot.Count);

            foreach (var del in asyncSnapshot)
            {
                if (del is Func<TEvent, Task> func)
                {
                    tasks.Add(SafeInvokeAsync(func, evt));
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }
    }

    // ===================== Publish async =====================
    public bool HasSubscribers<TEvent>()
    {
        lock (_lock)
        {
            var type = typeof(TEvent);
            return _syncHandlers.TryGetValue(type, out var list) && list.Count > 0;
        }
    }

    public bool HasAsyncSubscribers<TEvent>()
    {
        lock (_lock)
        {
            var type = typeof(TEvent);
            return _asyncHandlers.TryGetValue(type, out var list) && list.Count > 0;
        }
    }

    private static async Task SafeInvokeAsync<TEvent>(Func<TEvent, Task> handler, TEvent evt)
    {
        // Здесь можно повесить try/catch и логгер
        await handler(evt).ConfigureAwait(false);
    }

    // ===================== Subsriptions =====================
    private sealed class SyncSubscription<TEvent> : IDisposable
    {
        private readonly GameEventBusService _bus;
        private Action<TEvent> _handler;

        public SyncSubscription(GameEventBusService bus, Action<TEvent> handler)
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

    private sealed class AsyncSubscription<TEvent> : IDisposable
    {
        private readonly GameEventBusService _bus;
        private Func<TEvent, Task> _handler;

        public AsyncSubscription(GameEventBusService bus, Func<TEvent, Task> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_handler != null)
            {
                _bus.UnsubscribeAsync(_handler);
                _handler = null;
            }
        }
    }
}
