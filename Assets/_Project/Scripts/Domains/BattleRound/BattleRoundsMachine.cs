using System;
using System.Linq;
using Stateless;
using UnityEngine;

public sealed class BattleRoundsMachine
{
    private readonly IBattleContext _ctx;
    private readonly StateMachine<BattleRoundState, BattleRoundTrigger> _sm;

    public BattleRoundsMachine(IBattleContext ctx)
    {
        _ctx = ctx;
        _sm = new StateMachine<BattleRoundState, BattleRoundTrigger>(BattleRoundState.RoundInit);

        _sm.Configure(BattleRoundState.RoundInit)
            .OnEntry(RoundInit)
            .Permit(BattleRoundTrigger.BeginRound, BattleRoundState.TurnSelect);

        _sm.Configure(BattleRoundState.TurnSelect)
            .OnEntry(TurnSelect)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnStart)
            .Permit(BattleRoundTrigger.Skip, BattleRoundState.TurnSkip)
            .Permit(BattleRoundTrigger.QueueEmpty, BattleRoundState.RoundEnd);

        _sm.Configure(BattleRoundState.TurnStart)
            .OnEntry(TurnStart)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnActionHost)
            .Permit(BattleRoundTrigger.Skip, BattleRoundState.TurnSkip);

        _sm.Configure(BattleRoundState.TurnActionHost)
            .OnEntry(TurnActionHost)
            .Permit(BattleRoundTrigger.ActionDone, BattleRoundState.TurnEnd)
            .Permit(BattleRoundTrigger.Skip, BattleRoundState.TurnSkip);

        _sm.Configure(BattleRoundState.TurnSkip)
            .OnEntry(TurnSkip)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnEnd);

        _sm.Configure(BattleRoundState.TurnEnd)
            .OnEntry(TurnEnd)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnSelect)
            .Permit(BattleRoundTrigger.QueueEmpty, BattleRoundState.RoundEnd);

        _sm.Configure(BattleRoundState.RoundEnd)
            .OnEntry(RoundEnd)
            .Permit(BattleRoundTrigger.EndRound, BattleRoundState.RoundInit)
            .PermitReentry(BattleRoundTrigger.EndBattleRounds); // будет обработано фазовой машиной
    }

    public BattleRoundState State => _sm.State;

    public void Reset() => _sm.Activate();

    public void BeginRound() => _sm.Fire(BattleRoundTrigger.BeginRound);

    public void NextTurn() => _sm.Fire(BattleRoundTrigger.NextTurn);

    public void SkipTurn()
    {
        if (!_sm.CanFire(BattleRoundTrigger.Skip))
            return;

        if (!CanPlayerControlActiveUnit())
            return;

        var skipAction = new SkipTurnAction();
        AttachAction(skipAction);
        skipAction.Resolve();
    }

    public void DefendActiveUnit()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            Debug.LogWarning("[CombatLoop] Cannot defend without a BattleQueueController.");
            return;
        }

        if (!_sm.CanFire(BattleRoundTrigger.Skip))
            return;

        var activeUnit = _ctx.ActiveUnit;
        if (activeUnit == null)
            return;

        if (!IsFriendlyUnit(activeUnit))
            return;

        var defendAction = new DefendAction();
        AttachAction(defendAction);
        defendAction.Resolve();
    }

    private void RoundInit()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController != null)
        {
            var units = _ctx.BattleUnits;
            var unitModels = units == null
                ? Array.Empty<IReadOnlySquadModel>()
                : units
                    .Where(unit => unit != null)
                    .Select(unit => unit.GetSquadModel())
                    .Where(model => model != null);

            queueController.Rebuild(unitModels);
            _ctx.BattleQueueUIController?.Render(queueController);
        }
        else
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
        }

        _sm.Fire(BattleRoundTrigger.BeginRound);
    }

    private void TurnSelect()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundTrigger.QueueEmpty);
            return;
        }

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundTrigger.QueueEmpty);
            return;
        }

        var nextUnit = queue[0];
        if (nextUnit?.UnitDefinition != null)
        {
            string unitName = string.IsNullOrWhiteSpace(nextUnit.UnitDefinition.UnitName)
                ? nextUnit.UnitDefinition.name
                : nextUnit.UnitDefinition.UnitName;
            Debug.Log($"[CombatLoop] Active unit: {unitName}");
        }
        else
        {
            Debug.Log("[CombatLoop] Active unit: <unknown>");
        }

        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void TurnStart()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            Debug.LogWarning("[CombatLoop] BattleQueueController is missing in context.");
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundTrigger.NextTurn);
            return;
        }

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundTrigger.NextTurn);
            return;
        }

        _ctx.ActiveUnit = queue[0];

        _sm.Fire(BattleRoundTrigger.NextTurn); // к действию
    }

    private void TurnActionHost()
    {
        if (!CanPlayerControlActiveUnit())
        {
            AttachEnemySkipAction();
            return;
        }

        AttachNewAttackAction();
    }

    private void AttachNewAttackAction()
    {
        var attackAction = new AttackAction(_ctx);
        AttachAction(attackAction);
    }

    private void AttachEnemySkipAction()
    {
        var skipAction = new AutoSkipTurnAction(EnemyAutoSkipDelaySeconds);
        AttachAction(skipAction);
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

    private void AttachAction(IBattleAction action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        DetachCurrentAction();

        _ctx.CurrentAction = action;
        action.OnResolve += OnActionResolved;
        action.OnCancel += OnActionCancelled;
    }

    private void OnActionResolved()
    {
        var resolvedAction = _ctx.CurrentAction;

        DetachCurrentAction();

        switch (resolvedAction)
        {
            case DefendAction:
                var defendingUnit = _ctx.ActiveUnit;
                var queueController = _ctx.BattleQueueController;

                if (defendingUnit != null && queueController != null)
                {
                    queueController.AddLast(defendingUnit);
                }
                _sm.Fire(BattleRoundTrigger.Skip);
                break;
            case SkipTurnAction:
            case AutoSkipTurnAction:
                _sm.Fire(BattleRoundTrigger.Skip);
                break;
            default:
                _sm.Fire(BattleRoundTrigger.ActionDone);
                break;
        }
    }

    private void OnActionCancelled()
    {
        if (!CanPlayerControlActiveUnit())
            return;

        AttachNewAttackAction();
    }

    private void TurnSkip()
    {
        // логируем пропуск, публикуем события
        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void TurnEnd()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController == null)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundTrigger.QueueEmpty);
            return;
        }

        queueController.NextTurn();
        _ctx.ActiveUnit = null;

        _ctx.BattleQueueUIController?.Render(queueController);

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _sm.Fire(BattleRoundTrigger.QueueEmpty);
            return;
        }

        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void RoundEnd()
    {
        // onRoundEnd(), проверка конца боя:
        // если бой завершён → фазовая машина должна вызвать EndCombat
        // иначе новый раунд:
        _sm.Fire(BattleRoundTrigger.EndRound);
    }

    private const float EnemyAutoSkipDelaySeconds = 2f;

    private bool CanPlayerControlActiveUnit()
    {
        var activeUnit = _ctx.ActiveUnit;
        if (activeUnit == null)
            return false;

        return IsFriendlyUnit(activeUnit);
    }

    private static bool IsFriendlyUnit(IReadOnlySquadModel unit)
    {
        if (unit?.UnitDefinition == null)
            return false;

        return unit.UnitDefinition.Type is UnitType.Hero or UnitType.Ally;
    }
}
