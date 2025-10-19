using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RoundState : State<BattleContext>
{
    private CancellationTokenSource _queueLoopCancellation;
    private Task _queueLoopTask;
    private bool _isSubscribedToApplicationQuit;

    public override void Enter(BattleContext context)
    {
        context.PanelController?.Show(nameof(RoundState));

        StartQueueLoop(context);
    }

    public override void Exit(BattleContext context)
    {
        StopQueueLoop();
    }

    private void StartQueueLoop(BattleContext context)
    {
        StopQueueLoop();

        if (!_isSubscribedToApplicationQuit)
        {
            Application.quitting += OnApplicationQuitting;
            _isSubscribedToApplicationQuit = true;
        }

        _queueLoopCancellation = new CancellationTokenSource();
        _queueLoopTask = RunQueueLoopAsync(context, _queueLoopCancellation.Token);
    }

    private void StopQueueLoop()
    {
        if (_isSubscribedToApplicationQuit)
        {
            Application.quitting -= OnApplicationQuitting;
            _isSubscribedToApplicationQuit = false;
        }

        if (_queueLoopCancellation == null)
            return;

        var cancellation = _queueLoopCancellation;
        var task = _queueLoopTask;

        _queueLoopCancellation = null;
        _queueLoopTask = null;

        cancellation.Cancel();

        if (task != null)
        {
            task.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        Debug.LogException(t.Exception);

                    cancellation.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        else
        {
            cancellation.Dispose();
        }
    }

    private async Task RunQueueLoopAsync(BattleContext context, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                var units = BuildBattleUnits(context);
                var queue = BattleTurnQueueCalculator.CreateQueue(units);

                while (!cancellationToken.IsCancellationRequested && Application.isPlaying && queue.Count > 0)
                {
                    var currentUnit = queue.Peek();
                    var definition = currentUnit?.UnitModel?.Definition;
                    var unitName = definition?.UnitName ?? "Unknown";
                    var unitType = definition?.Type.ToString() ?? "Unknown";

                    Debug.Log($"Processing turn: {unitName} ({unitType})");
                    Debug.Log("Waiting for queue...");

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

                    Debug.Log("Turn finished");

                    queue.Dequeue();
                }

                if (!Application.isPlaying)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static BattleUnitModel[] BuildBattleUnits(BattleContext context)
    {
        if (context?.Payload == null)
            return Array.Empty<BattleUnitModel>();

        var units = new List<BattleUnitModel>();

        AddUnitIfPresent(context.Payload.Hero, units);
        AddArmyUnits(context.Payload.Army, units);
        AddUnitIfPresent(context.Payload.Enemy, units);

        return units.ToArray();
    }

    private static void AddUnitIfPresent(IReadOnlyUnitModel unit, List<BattleUnitModel> units)
    {
        if (unit == null)
            return;

        units.Add(new BattleUnitModel(unit));
    }

    private static void AddArmyUnits(IReadOnlyArmyModel army, List<BattleUnitModel> units)
    {
        if (army == null)
            return;

        var slots = army.GetAllSlots();
        if (slots == null)
            return;

        foreach (var squad in slots)
        {
            if (squad == null || squad.IsEmpty)
                continue;

            var definition = squad.UnitDefinition;
            if (definition == null)
                continue;

            for (int i = 0; i < squad.Count; i++)
            {
                var unitModel = new UnitModel(definition);
                units.Add(new BattleUnitModel(unitModel));
            }
        }
    }

    private void OnApplicationQuitting()
    {
        StopQueueLoop();
    }
}
