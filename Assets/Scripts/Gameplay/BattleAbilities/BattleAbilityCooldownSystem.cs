using System;
using System.Collections.Generic;

public sealed class BattleAbilityCooldownSystem : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();
    private readonly BattleContext _ctx;

    public BattleAbilityCooldownSystem(BattleContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        var bus = _ctx.SceneEventBusService ?? throw new ArgumentNullException(nameof(_ctx.SceneEventBusService));

        _subscriptions.Add(bus.Subscribe<BattleRoundStarted>(_ => _ctx.BattleAbilitiesManager.OnTick()));
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
