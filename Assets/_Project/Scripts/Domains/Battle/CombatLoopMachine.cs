using System;
using System.Linq;
using System.Threading.Tasks;
using Stateless;
using UnityEngine;

public sealed class CombatLoopMachine
{
    private static readonly TimeSpan TurnDelay = TimeSpan.FromSeconds(2);

    private readonly IBattleContext _ctx;
    private readonly StateMachine<CombatState, CombatTrigger> _sm;
    private readonly ActionPipelineMachine _action;

    public CombatLoopMachine(IBattleContext ctx, ActionPipelineMachine action)
    {
        _ctx = ctx;
        _action = action;
        _sm = new StateMachine<CombatState, CombatTrigger>(CombatState.RoundInit);

        _sm.Configure(CombatState.RoundInit)
            .OnEntry(RoundInit)
            .Permit(CombatTrigger.BeginRound, CombatState.TurnSelect);

        _sm.Configure(CombatState.TurnSelect)
            .OnEntry(TurnSelect)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnStart)
            .Permit(CombatTrigger.Skip, CombatState.TurnSkip)
            .Permit(CombatTrigger.QueueEmpty, CombatState.RoundEnd);

        _sm.Configure(CombatState.TurnStart)
            .OnEntry(TurnStart)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnActionHost);

        _sm.Configure(CombatState.TurnActionHost)
            .OnEntry(TurnActionHost)
            .Permit(CombatTrigger.ActionDone, CombatState.TurnEnd);

        _sm.Configure(CombatState.TurnSkip)
            .OnEntry(TurnSkip)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnEnd);

        _sm.Configure(CombatState.TurnEnd)
            .OnEntry(TurnEnd)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnSelect)
            .Permit(CombatTrigger.QueueEmpty, CombatState.RoundEnd);

        _sm.Configure(CombatState.RoundEnd)
            .OnEntry(RoundEnd)
            .Permit(CombatTrigger.EndRound, CombatState.RoundInit)
            .PermitReentry(CombatTrigger.EndCombat); // будет обработано фазовой машиной
    }

    public CombatState State => _sm.State;

    public void Reset() => _sm.Activate(); // опционально
    public void BeginRound() => _sm.Fire(CombatTrigger.BeginRound);
    public void NextTurn() => _sm.Fire(CombatTrigger.NextTurn);

    // ---- Handlers ----
    private void RoundInit()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController != null)
        {
            var units = _ctx.BattleUnits;
            var unitModels = units == null
                ? Array.Empty<IReadOnlyUnitModel>()
                : units
                    .Where(unit => unit != null)
                    .Select(unit => unit.GetUnitModel())
                    .Where(model => model != null)
                    .Cast<IReadOnlyUnitModel>();

            queueController.Rebuild(unitModels);
            _ctx.BattleQueueUIController?.Update(queueController);
        }
        else
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
        }

        _sm.Fire(CombatTrigger.BeginRound);
    }

    private void TurnSelect()
    {
        _ = HandleTurnSelectAsync();
    }

    private void TurnStart()
    {
        _sm.Fire(CombatTrigger.NextTurn); // к действию
    }

    private void TurnActionHost()
    {
        _ = HandleTurnActionHostAsync();
    }

    private void TurnSkip()
    {
        // логируем пропуск, публикуем события
        _sm.Fire(CombatTrigger.NextTurn);
    }

    private void TurnEnd()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            _sm.Fire(CombatTrigger.QueueEmpty);
            return;
        }

        queueController.NextTurn();
        _ctx.BattleQueueUIController?.Update(queueController);

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _sm.Fire(CombatTrigger.QueueEmpty);
            return;
        }

        _sm.Fire(CombatTrigger.NextTurn);
    }

    private void RoundEnd()
    {
        // onRoundEnd(), проверка конца боя:
        // если бой завершён → фазовая машина должна вызвать EndCombat
        // иначе новый раунд:
        _sm.Fire(CombatTrigger.EndRound);
    }

    private async Task HandleTurnSelectAsync()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
            _sm.Fire(CombatTrigger.QueueEmpty);
            return;
        }

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _sm.Fire(CombatTrigger.QueueEmpty);
            return;
        }

        var nextUnit = queue[0];
        if (nextUnit?.Definition != null)
        {
            string unitName = string.IsNullOrWhiteSpace(nextUnit.Definition.UnitName)
                ? nextUnit.Definition.name
                : nextUnit.Definition.UnitName;
            Debug.Log($"[CombatLoop] Active unit: {unitName}");
        }
        else
        {
            Debug.Log("[CombatLoop] Active unit: <unknown>");
        }

        await Task.Delay(TurnDelay);

        _sm.Fire(CombatTrigger.NextTurn);
    }

    private async Task HandleTurnActionHostAsync()
    {
        await Task.Delay(TurnDelay);
        _sm.Fire(CombatTrigger.ActionDone);
    }
}
