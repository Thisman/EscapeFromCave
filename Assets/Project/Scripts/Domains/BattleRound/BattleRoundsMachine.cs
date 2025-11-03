using System;
using System.Collections.Generic;
using System.Linq;
using Stateless;
using UnityEngine;

public sealed class BattleRoundsMachine
{
    private readonly BattleContext _ctx;
    private readonly StateMachine<BattleRoundState, BattleRoundTrigger> _sm;
    private bool _battleFinished;
    private bool _playerRequestedFlee;

    public event Action<BattleResult> OnBattleRoundsFinished;

    public BattleRoundsMachine(BattleContext ctx)
    {
        _ctx = ctx;
        _sm = new StateMachine<BattleRoundState, BattleRoundTrigger>(BattleRoundState.RoundInit);

        _sm.Configure(BattleRoundState.RoundInit)
            .OnEntry(OnRoundInit)
            .Permit(BattleRoundTrigger.InitTurn, BattleRoundState.TurnInit);

        _sm.Configure(BattleRoundState.TurnInit)
            .OnEntry(OnTurnInit)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnStart)
            .Permit(BattleRoundTrigger.SkipTurn, BattleRoundState.TurnSkip)
            .Permit(BattleRoundTrigger.EndRound, BattleRoundState.RoundEnd);

        _sm.Configure(BattleRoundState.TurnStart)
            .OnEntry(OnTurnStart)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnWaitAction)
            .Permit(BattleRoundTrigger.SkipTurn, BattleRoundState.TurnSkip);

        _sm.Configure(BattleRoundState.TurnWaitAction)
            .OnEntry(OnWaitTurnAction)
            .Permit(BattleRoundTrigger.ActionDone, BattleRoundState.TurnEnd)
            .Permit(BattleRoundTrigger.SkipTurn, BattleRoundState.TurnSkip);

        _sm.Configure(BattleRoundState.TurnSkip)
            .OnEntry(OnTurnSkip)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnEnd);

        _sm.Configure(BattleRoundState.TurnEnd)
            .OnEntry(OnTurnEnd)
            .Permit(BattleRoundTrigger.NextTurn, BattleRoundState.TurnInit)
            .Permit(BattleRoundTrigger.EndRound, BattleRoundState.RoundEnd);

        _sm.Configure(BattleRoundState.RoundEnd)
            .OnEntry(OnRoundEnd)
            .Permit(BattleRoundTrigger.StartNewRound, BattleRoundState.RoundInit);
    }

    public void Reset()
    {
        _battleFinished = false;
        _playerRequestedFlee = false;
        _sm.Activate();
        _ctx.BattleCombatUIController.OnLeaveCombat += HandleLeaveCombat;
    }

    public void BeginRound() => _sm.Fire(BattleRoundTrigger.InitTurn);

    private void OnRoundInit()
    {
        _ctx.DefendedUnitsThisRound?.Clear();

        var unitModels = _ctx.BattleUnits
                .Where(unit => unit != null)
                .Select(unit => unit.GetSquadModel())
                .Where(model => model != null);

        _ctx.BattleQueueController.Build(unitModels);
        _ctx.BattleQueueUIController.Render(_ctx.BattleQueueController);

        _sm.Fire(BattleRoundTrigger.InitTurn);
    }

    private void OnTurnInit()
    {
        var queue = _ctx.BattleQueueController.GetQueue();
        if (queue.Count == 0)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundTrigger.EndRound);
            return;
        }

        _ctx.BattleQueueUIController.Render(_ctx.BattleQueueController);
        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnTurnStart()
    {
        var queue = _ctx.BattleQueueController.GetQueue();
        _ctx.ActiveUnit = queue[0];

        _ctx.BattleEffectsManager.OnTick();

        var abilities = _ctx.ActiveUnit.Abilities;
        _ctx.BattleCombatUIController?.RenderAbilityList(abilities);

        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnWaitTurnAction()
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
        _ctx.BattleQueueController.NextTurn();
        _ctx.ActiveUnit = null;

        RemoveDefeatedUnits(_ctx.BattleQueueController, _ctx.BattleGridController);

        if (CheckForBattleCompletion(_ctx.BattleQueueController))
        {
            TriggerBattleFinish();
            return;
        }

        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnRoundEnd()
    {
        if (_battleFinished)
            return;

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

        if (action is AbilityAction abilityAction)
        {
            _ctx.BattleCombatUIController?.HighlightAbility(abilityAction.Ability);
        }
        else
        {
            _ctx.BattleCombatUIController?.ResetAbilityHighlight();
        }
    }

    private void DetachCurrentAction()
    {
        if (_ctx.CurrentAction == null)
            return;

        _ctx.CurrentAction.OnResolve -= OnActionResolved;
        _ctx.CurrentAction.OnCancel -= OnActionCancelled;

        if (_ctx.CurrentAction is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _ctx.BattleCombatUIController?.ResetAbilityHighlight();

        _ctx.CurrentAction = null;
    }

    private void OnActionResolved()
    {
        var resolvedAction = _ctx.CurrentAction;

        DetachCurrentAction();

        switch (resolvedAction)
        {
            case DefendAction:
                _ctx.DefendedUnitsThisRound.Add(_ctx.ActiveUnit);
                _ctx.BattleQueueController.AddLast(_ctx.ActiveUnit);
                _sm.Fire(BattleRoundTrigger.SkipTurn);
                break;
            case SkipTurnAction:
                _sm.Fire(BattleRoundTrigger.SkipTurn);
                break;
            default:
                _sm.Fire(BattleRoundTrigger.ActionDone);
                break;
        }
    }

    private void OnActionCancelled()
    {
        if (!_ctx.ActiveUnit.IsFriendly())
            return;

        _ctx.BattleCombatUIController?.ResetAbilityHighlight();

        OnWaitTurnAction();
    }

    private bool CheckForBattleCompletion(BattleQueueController queueController)
    {
        if (_battleFinished)
            return true;

        IReadOnlyList<BattleSquadController> units = _ctx.BattleUnits;

        if (units.Count == 0)
            return true;

        bool heroInQueue = units.Any(unit => unit.GetSquadModel().IsHero());

        if (!heroInQueue)
            return true;

        bool hasFriendlyUnits = false;
        bool hasEnemyUnits = false;

        foreach (var unitController in units)
        {
            if (unitController == null)
                continue;

            var model = unitController.GetSquadModel();

            if (model == null || model.Count <= 0)
                continue;

            if (model.IsFriendly())
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
            return true;

        if (hasFriendlyUnits == hasEnemyUnits)
            return false;

        return true;
    }

    private void TriggerBattleFinish()
    {
        if (_battleFinished)
            return;

        _battleFinished = true;

        _ctx.BattleCombatUIController.OnLeaveCombat -= HandleLeaveCombat;
        _ctx.BattleQueueController.Build(Array.Empty<IReadOnlySquadModel>());

        if (_sm.CanFire(BattleRoundTrigger.EndRound))
        {
            _sm.Fire(BattleRoundTrigger.EndRound);
        }

        var unitsResult = BuildUnitsResult();
        var status = DetermineBattleStatus(unitsResult);
        var result = new BattleResult(status, unitsResult);
        _ctx.BattleResult = result;

        OnBattleRoundsFinished?.Invoke(result);
    }

    private void RemoveDefeatedUnits(BattleQueueController queueController, BattleGridController gridController)
    {
        if (_ctx.BattleUnits.Count == 0)
        {
            _ctx.BattleUnits = Array.Empty<BattleSquadController>();
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

            while (queueController.Remove(model))
            {
                // Empty body, need refactoring later
            }

            var defeatedTransform = defeatedUnit.transform;
            gridController.TryRemoveOccupant(defeatedTransform, out _);
            UnityEngine.Object.Destroy(defeatedUnit.gameObject);
        }
    }

    private void HandleLeaveCombat()
    {
        _playerRequestedFlee = true;
        TriggerBattleFinish();
    }

    private BattleUnitsResult BuildUnitsResult()
    {
        var units = _ctx.BattleUnits;

        if (units == null || units.Count == 0)
        {
            return new BattleUnitsResult(
                Array.Empty<IReadOnlySquadModel>(),
                Array.Empty<IReadOnlySquadModel>());
        }

        var friendlyUnits = new List<IReadOnlySquadModel>();
        var enemyUnits = new List<IReadOnlySquadModel>();

        foreach (var unitController in units)
        {
            if (unitController == null)
                continue;

            var model = unitController.GetSquadModel();

            if (model == null || model.Count <= 0)
                continue;

            switch (model.Kind)
            {
                case UnitKind.Hero:
                case UnitKind.Ally:
                    friendlyUnits.Add(model);
                    break;
                case UnitKind.Enemy:
                    enemyUnits.Add(model);
                    break;
            }
        }

        return new BattleUnitsResult(
            friendlyUnits.Count > 0 ? friendlyUnits.ToArray() : Array.Empty<IReadOnlySquadModel>(),
            enemyUnits.Count > 0 ? enemyUnits.ToArray() : Array.Empty<IReadOnlySquadModel>());
    }

    private BattleResultStatus DetermineBattleStatus(BattleUnitsResult unitsResult)
    {
        if (_playerRequestedFlee)
            return BattleResultStatus.Flee;

        bool heroAlive = unitsResult.FriendlyUnits.Any(model => model.IsHero());

        if (!heroAlive)
            return BattleResultStatus.Defeat;

        if (unitsResult.FriendlyUnits.Count > 0 && unitsResult.EnemyUnits.Count == 0)
            return BattleResultStatus.Victory;

        if (unitsResult.FriendlyUnits.Count > 0)
            return BattleResultStatus.Victory;

        return BattleResultStatus.Defeat;
    }
}
