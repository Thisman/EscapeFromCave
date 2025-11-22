using System;
using System.Collections.Generic;

public sealed class AbilityCooldownSystem : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();
    private readonly BattleContext _ctx;

    public AbilityCooldownSystem(BattleContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        var bus = _ctx.SceneEventBusService ?? throw new ArgumentNullException(nameof(_ctx.SceneEventBusService));

        _subscriptions.Add(bus.Subscribe<RoundStartedEvent>(_ => _ctx.BattleAbilitiesManager.OnTick()));
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }

        _subscriptions.Clear();
    }
}
