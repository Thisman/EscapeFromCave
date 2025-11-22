using System;
using System.Collections.Generic;
using System.Linq;

public sealed class BattleQueueSystem : IDisposable
{
    private readonly BattleContext _ctx;
    private readonly List<IDisposable> _subscriptions = new();

    public BattleQueueSystem(BattleContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

        var bus = _ctx.SceneEventBusService ?? throw new ArgumentNullException(nameof(_ctx.SceneEventBusService));
        _subscriptions.Add(bus.Subscribe<RoundStartedEvent>(OnRoundStarted));
        _subscriptions.Add(bus.Subscribe<TurnEndedEvent>(OnTurnEnded));
        _subscriptions.Add(bus.Subscribe<BattleFinishedEvent>(OnBattleFinished));
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }

        _subscriptions.Clear();
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        var unitModels = _ctx.BattleUnits
            .Where(unit => unit != null)
            .Select(unit => unit.GetSquadModel())
            .Where(model => model != null);

        _ctx.BattleQueueController.Build(unitModels);
        _ctx.BattleSceneUIController.RenderQueue(_ctx.BattleQueueController);
    }

    private void OnTurnEnded(TurnEndedEvent evt)
    {
        _ctx.BattleQueueController.NextTurn();
        RemoveDefeatedUnits(_ctx.BattleQueueController, _ctx.BattleGridController);
        _ctx.BattleSceneUIController.RenderQueue(_ctx.BattleQueueController);
    }

    private void OnBattleFinished(BattleFinishedEvent evt)
    {
        _ctx.BattleQueueController.Build(Array.Empty<IReadOnlySquadModel>());
        _ctx.BattleSceneUIController.RenderQueue(_ctx.BattleQueueController);
    }

    private void RemoveDefeatedUnits(BattleQueueController queueController, BattleGridController gridController)
    {
        if (_ctx.BattleUnits.Count == 0)
        {
            _ctx.RegisterSquads(Array.Empty<BattleSquadController>());
            return;
        }

        var aliveUnits = new List<BattleSquadController>(_ctx.BattleUnits.Count);
        var defeatedUnits = new List<BattleSquadController>();

        foreach (var unitController in _ctx.BattleUnits)
        {
            if (unitController == null)
                continue;

            var model = unitController.GetSquadModel();

            if (model.Count > 0)
            {
                aliveUnits.Add(unitController);
            }
            else
            {
                defeatedUnits.Add(unitController);
            }
        }

        _ctx.RegisterSquads(aliveUnits);

        foreach (var defeatedUnit in defeatedUnits)
        {
            if (defeatedUnit == null)
                continue;

            var model = defeatedUnit.GetSquadModel();

            while (queueController.Remove(model))
            {
                // Empty body, need refactoring later
            }

            var defeatedTransform = defeatedUnit.transform;
            gridController.TryRemoveOccupant(defeatedTransform, out _);
        }
    }
}
