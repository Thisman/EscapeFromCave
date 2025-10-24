using System;
using System.Linq;
using Stateless;
using UnityEngine;

public sealed class CombatLoopMachine
{
    private readonly IBattleContext _ctx;
    private readonly StateMachine<CombatState, CombatTrigger> _sm;

    public CombatLoopMachine(IBattleContext ctx)
    {
        _ctx = ctx;
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
            .Permit(CombatTrigger.NextTurn, CombatState.TurnActionHost)
            .Permit(CombatTrigger.Skip, CombatState.TurnSkip);

        _sm.Configure(CombatState.TurnActionHost)
            .OnEntry(TurnActionHost)
            .Permit(CombatTrigger.ActionDone, CombatState.TurnEnd)
            .Permit(CombatTrigger.Skip, CombatState.TurnSkip);

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

    public void Reset() => _sm.Activate();

    public void BeginRound() => _sm.Fire(CombatTrigger.BeginRound);

    public void NextTurn() => _sm.Fire(CombatTrigger.NextTurn);

    public void SkipTurn()
    {
        if (!_sm.CanFire(CombatTrigger.Skip))
            return;

        _sm.Fire(CombatTrigger.Skip);
    }

    public void DefendActiveUnit()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            Debug.LogWarning("[CombatLoop] Cannot defend without a BattleQueueController.");
            return;
        }

        if (!_sm.CanFire(CombatTrigger.Skip))
            return;

        if (_defendingUnit != null)
            return;

        var activeUnit = _ctx.ActiveUnit;
        if (activeUnit == null)
            return;

        _defendingUnit = activeUnit;
        _sm.Fire(CombatTrigger.Skip);
    }

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
            _ctx.BattleQueueUIController?.Render(queueController);
        }
        else
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
        }

        _sm.Fire(CombatTrigger.BeginRound);
    }

    private void TurnSelect()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
            _ctx.ActiveUnit = null;
            _sm.Fire(CombatTrigger.QueueEmpty);
            return;
        }

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _ctx.ActiveUnit = null;
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

        _sm.Fire(CombatTrigger.NextTurn);
    }

    private void TurnStart()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
            _ctx.ActiveUnit = null;
            _sm.Fire(CombatTrigger.NextTurn);
            return;
        }

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(CombatTrigger.NextTurn);
            return;
        }

        _ctx.ActiveUnit = queue[0];

        _sm.Fire(CombatTrigger.NextTurn); // к действию
    }

    private void TurnActionHost()
    {
        AttachNewAttackAction();
    }

    private void AttachNewAttackAction()
    {
        DetachCurrentAction();

        var attackAction = new AttackAction(_ctx);
        _ctx.CurrentAction = attackAction;
        attackAction.OnResolve += OnActionResolved;
        attackAction.OnCancel += OnActionCancelled;
    }

    private void DetachCurrentAction()
    {
        var currentAction = _ctx.CurrentAction;
        if (currentAction == null)
            return;

        currentAction.OnResolve -= OnActionResolved;
        currentAction.OnCancel -= OnActionCancelled;

        if (currentAction is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _ctx.CurrentAction = null;
    }

    private void OnActionResolved()
    {
        ActionDone();
    }

    private void OnActionCancelled()
    {
        AttachNewAttackAction();
    }

    private void ActionDone()
    {
        DetachCurrentAction();
        _sm.Fire(CombatTrigger.ActionDone);
    }

    private void TurnSkip()
    {
        // логируем пропуск, публикуем события
        _sm.Fire(CombatTrigger.NextTurn);
    }

    private IReadOnlyUnitModel _defendingUnit;

    private void TurnEnd()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(CombatTrigger.QueueEmpty);
            return;
        }

        var finishedUnit = queueController.NextTurn();

        if (finishedUnit != null && _defendingUnit != null && ReferenceEquals(finishedUnit, _defendingUnit))
        {
            queueController.AddLast(finishedUnit);
        }

        _defendingUnit = null;
        _ctx.ActiveUnit = null;

        _ctx.BattleQueueUIController?.Render(queueController);

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
}
