using System;
using System.Collections.Generic;
using System.Linq;
using Stateless;
using UnityEngine;

public sealed class BattleRoundsMachine
{
    private readonly IBattleContext _ctx;
    private readonly StateMachine<BattleRoundState, BattleRoundTrigger> _sm;
    private bool _battleFinished;

    public event Action BattleFinished;

    public BattleRoundsMachine(IBattleContext ctx)
    {
        _ctx = ctx;
        _sm = new StateMachine<BattleRoundState, BattleRoundTrigger>(BattleRoundState.RoundInit);

        _sm.Configure(BattleRoundState.RoundInit)
            .OnEntry(OnRoundInit)
            .Permit(BattleRoundTrigger.StartRound, BattleRoundState.TurnSelect);

        _sm.Configure(BattleRoundState.TurnSelect)
            .OnEntry(OnTurnSelect)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnStart)
            .Permit(BattleRoundTrigger.SkipTurn, BattleRoundState.TurnSkip)
            .Permit(BattleRoundTrigger.QueueEmpty, BattleRoundState.RoundEnd);

        _sm.Configure(BattleRoundState.TurnStart)
            .OnEntry(OnTurnStart)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnActionWait)
            .Permit(BattleRoundTrigger.SkipTurn, BattleRoundState.TurnSkip);

        _sm.Configure(BattleRoundState.TurnActionWait)
            .OnEntry(OnTurnActionWait)
            .Permit(BattleRoundTrigger.ActionDone, BattleRoundState.TurnEnd)
            .Permit(BattleRoundTrigger.SkipTurn, BattleRoundState.TurnSkip);

        _sm.Configure(BattleRoundState.TurnSkip)
            .OnEntry(OnTurnSkip)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnEnd);

        _sm.Configure(BattleRoundState.TurnEnd)
            .OnEntry(OnTurnEnd)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnSelect)
            .Permit(BattleRoundTrigger.QueueEmpty, BattleRoundState.RoundEnd);

        _sm.Configure(BattleRoundState.RoundEnd)
            .OnEntry(OnRoundEnd)
            .Permit(BattleRoundTrigger.StartNewRound, BattleRoundState.RoundInit);
    }

    public BattleRoundState State => _sm.State;

    public void Reset()
    {
        _battleFinished = false;
        _sm.Activate();
    }

    public void BeginRound() => _sm.Fire(BattleRoundTrigger.StartRound);

    private void OnRoundInit()
    {
        _ctx.BattleCombatUIController.OnLeaveCombat += HandleLeaveCombat;

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

        _sm.Fire(BattleRoundTrigger.StartRound);
    }

    private void OnTurnSelect()
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

    private void OnTurnStart()
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

        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnTurnActionWait()
    {
        IReadOnlySquadModel activeUnit = _ctx.ActiveUnit;
        BattleActionControllerResolver resolver = _ctx.BattleActionControllerResolver;
        IBattleActionController controller = resolver.ResolveFor(activeUnit);

        controller.RequestAction(_ctx, action =>
        {
            if (action == null)
            {
                Debug.LogWarning("[CombatLoop] Battle action controller returned no action. Skipping turn.");
                _sm.Fire(BattleRoundTrigger.SkipTurn);
                return;
            }

            try
            {
                AttachAction(action);
                action.Resolve();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                DetachCurrentAction();
                _sm.Fire(BattleRoundTrigger.SkipTurn);
            }
        });
    }

    private void OnTurnSkip()
    {
        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnTurnEnd()
    {
        var queueController = _ctx.BattleQueueController;

        if (queueController != null)
        {
            queueController.NextTurn();
        }

        _ctx.ActiveUnit = null;

        RemoveDefeatedUnits(queueController, _ctx.BattleGridController);

        if (CheckForBattleCompletion(queueController))
            return;

        if (queueController == null)
        {
            _sm.Fire(BattleRoundTrigger.QueueEmpty);
            return;
        }

        _ctx.BattleQueueUIController?.Render(queueController);

        var queue = queueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _sm.Fire(BattleRoundTrigger.QueueEmpty);
            return;
        }

        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnRoundEnd()
    {
        if (_battleFinished)
            return;

        _ctx.BattleCombatUIController.OnLeaveCombat -= HandleLeaveCombat;
        _sm.Fire(BattleRoundTrigger.StartNewRound);
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
                _sm.Fire(BattleRoundTrigger.SkipTurn);
                break;
            case SkipTurnAction:
            case AutoSkipTurnAction:
                _sm.Fire(BattleRoundTrigger.SkipTurn);
                break;
            default:
                _sm.Fire(BattleRoundTrigger.ActionDone);
                break;
        }
    }

    private void OnActionCancelled()
    {
        if (!CanPlayerControlActiveUnit(_ctx.ActiveUnit))
            return;

        OnTurnActionWait();
    }

    private bool CheckForBattleCompletion(BattleQueueController queueController)
    {
        if (_battleFinished)
            return true;

        IReadOnlyList<BattleSquadController> units = _ctx.BattleUnits;

        if (units.Count == 0)
            return TriggerBattleFinish(queueController);

        bool heroInQueue = units.Any(unit => unit.GetSquadModel().UnitDefinition.Type == UnitType.Hero);

        if (!heroInQueue)
            return TriggerBattleFinish(queueController);

        bool hasFriendlyUnits = false;
        bool hasEnemyUnits = false;

        foreach (var unitController in units)
        {
            if (unitController == null)
                continue;

            var model = unitController.GetSquadModel();

            if (model == null || model.Count <= 0)
                continue;

            if (CanPlayerControlActiveUnit(model))
            {
                hasFriendlyUnits = true;
            }
            else
            {
                hasEnemyUnits = true;
            }

            if (hasFriendlyUnits && hasEnemyUnits)
                return false;
        }

        if (!hasFriendlyUnits && !hasEnemyUnits)
            return TriggerBattleFinish(queueController);

        if (hasFriendlyUnits == hasEnemyUnits)
            return false;

        return TriggerBattleFinish(queueController);
    }

    private bool TriggerBattleFinish(BattleQueueController queueController)
    {
        if (_battleFinished)
            return true;

        _battleFinished = true;

        if (queueController != null)
        {
            queueController.Rebuild(Array.Empty<IReadOnlySquadModel>());
        }

        if (_sm.CanFire(BattleRoundTrigger.QueueEmpty))
        {
            _sm.Fire(BattleRoundTrigger.QueueEmpty);
        }

        BattleFinished?.Invoke();

        return true;
    }

    private void RemoveDefeatedUnits(BattleQueueController queueController, BattleGridController gridController)
    {
        var units = _ctx.BattleUnits;
        if (units == null || units.Count == 0)
        {
            _ctx.BattleUnits = Array.Empty<BattleSquadController>();
            return;
        }

        var aliveUnits = new List<BattleSquadController>(units.Count);
        var defeatedUnits = new List<BattleSquadController>();

        foreach (var unitController in units)
        {
            if (unitController == null)
                continue;

            var model = unitController.GetSquadModel();

            if (model == null || model.Count > 0)
            {
                aliveUnits.Add(unitController);
                continue;
            }

            defeatedUnits.Add(unitController);
        }

        if (defeatedUnits.Count == 0)
            return;

        _ctx.BattleUnits = aliveUnits.Count > 0
            ? aliveUnits
            : Array.Empty<BattleSquadController>();

        foreach (var defeatedUnit in defeatedUnits)
        {
            if (defeatedUnit == null)
                continue;

            var model = defeatedUnit.GetSquadModel();

            if (queueController != null && model != null)
            {
                while (queueController.Remove(model))
                {
                }
            }

            if (gridController != null)
            {
                var defeatedTransform = defeatedUnit.transform;
                if (defeatedTransform != null)
                {
                    gridController.TryRemoveOccupant(defeatedTransform, out _);
                }
            }

            UnityEngine.Object.Destroy(defeatedUnit.gameObject);
        }
    }

    private void HandleLeaveCombat()
    {
        TriggerBattleFinish(_ctx.BattleQueueController);
    }

    private bool CanPlayerControlActiveUnit(IReadOnlySquadModel unit)
    {
        if (unit?.UnitDefinition == null)
            return false;

        return unit.UnitDefinition.Type is UnitType.Hero or UnitType.Ally;
    }
}
