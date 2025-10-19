using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RoundState : State<BattleContext>
{
    private CancellationTokenSource _queueLoopCancellation;
    private Task _queueLoopTask;

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

        _queueLoopCancellation = new CancellationTokenSource();
        _queueLoopTask = RunQueueLoopAsync(context, _queueLoopCancellation.Token);
    }

    private void StopQueueLoop()
    {
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
            while (!cancellationToken.IsCancellationRequested)
            {
                var units = BuildBattleUnits(context);
                BattleTurnQueueCalculator.CreateQueue(units);

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
        AddUnitIfPresent(context.Payload.Enemy, units);

        return units.ToArray();
    }

    private static void AddUnitIfPresent(IReadOnlyUnitModel unit, List<BattleUnitModel> units)
    {
        if (unit == null)
            return;

        units.Add(new BattleUnitModel(unit));
    }
}
