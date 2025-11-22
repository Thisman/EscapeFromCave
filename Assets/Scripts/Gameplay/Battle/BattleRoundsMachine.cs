using System;
using System.Collections.Generic;
using Stateless;
using UnityEngine;

public sealed class BattleRoundsMachine
{
    private bool _battleFinished;
    private bool _playerRequestedFlee;
    private int _currentRound;
    private int _currentActionId;
    private BattleResult _battleResult;
    private readonly BattleContext _ctx;
    private readonly StateMachine<BattleRoundStates, BattleRoundsTrigger> _sm;

    private readonly HashSet<IReadOnlySquadModel> _enemySquadSet = new();
    private readonly List<IReadOnlySquadModel> _enemySquadHistory = new();
    private readonly HashSet<IReadOnlySquadModel> _friendlySquadSet = new();
    private readonly List<IReadOnlySquadModel> _friendlySquadHistory = new();
    private readonly Dictionary<IReadOnlySquadModel, int> _initialSquadCounts = new();

    private readonly AIBattleActionController _enemyTurnController;
    private readonly PlayerBattleActionController _playerTurnController;
    private readonly ProviderForBattleActionController _actionControllerResolver;

    public BattleRoundsMachine(BattleContext ctx)
    {
        _ctx = ctx;
        _sm = new StateMachine<BattleRoundStates, BattleRoundsTrigger>(BattleRoundStates.RoundStart);

        _sm.Configure(BattleRoundStates.RoundStart)
            .OnEntry(OnRoundStart)
            .Permit(BattleRoundsTrigger.InitTurn, BattleRoundStates.TurnInit);

        _sm.Configure(BattleRoundStates.TurnInit)
            .OnEntry(OnTurnInit)
            .Permit(BattleRoundsTrigger.NextTurn, BattleRoundStates.TurnStart)
            .Permit(BattleRoundsTrigger.SkipTurn, BattleRoundStates.TurnSkip)
            .Permit(BattleRoundsTrigger.EndRound, BattleRoundStates.RoundEnd);

        _sm.Configure(BattleRoundStates.TurnStart)
            .OnEntry(OnTurnStart)
            .Permit(BattleRoundsTrigger.NextTurn, BattleRoundStates.TurnWaitAction)
            .Permit(BattleRoundsTrigger.SkipTurn, BattleRoundStates.TurnSkip);

        _sm.Configure(BattleRoundStates.TurnWaitAction)
            .OnEntry(OnWaitTurnAction)
            .Permit(BattleRoundsTrigger.ActionDone, BattleRoundStates.TurnEnd)
            .Permit(BattleRoundsTrigger.SkipTurn, BattleRoundStates.TurnSkip);

        _sm.Configure(BattleRoundStates.TurnSkip)
            .OnEntry(OnTurnSkip)
            .Permit(BattleRoundsTrigger.NextTurn, BattleRoundStates.TurnEnd);

        _sm.Configure(BattleRoundStates.TurnEnd)
            .OnEntry(OnTurnEnd)
            .Permit(BattleRoundsTrigger.NextTurn, BattleRoundStates.TurnInit)
            .Permit(BattleRoundsTrigger.EndRound, BattleRoundStates.RoundEnd);

        _sm.Configure(BattleRoundStates.RoundEnd)
            .OnEntry(OnRoundEnd)
            .Permit(BattleRoundsTrigger.StartNewRound, BattleRoundStates.RoundStart);

        _playerTurnController = new PlayerBattleActionController();
        _enemyTurnController = new AIBattleActionController();
        _actionControllerResolver = new ProviderForBattleActionController(_playerTurnController, _enemyTurnController);
    }

    public void Reset()
    {
        _battleFinished = false;
        _playerRequestedFlee = false;
        _battleResult = null;
        _currentRound = 0;
        _currentActionId = 0;
        InitializeSquadHistory();
        SubscribeToGameEvents();
    }

    public void BeginRounds() => OnRoundStart();

    private void OnRoundStart()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.RoundStart);
        _ctx.ActiveUnit = null;
        _currentRound++;

        _ctx.SceneEventBusService.Publish(new BattleRoundStarted(_currentRound));
        _sm.Fire(BattleRoundsTrigger.InitTurn);
    }

    private void OnTurnInit()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.TurnInit);
        var queue = _ctx.BattleQueueController.GetQueue();
        if (queue.Count == 0)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundsTrigger.EndRound);
            return;
        }

        _ctx.ActiveUnit = queue[0];
        _currentActionId++;
        _ctx.SceneEventBusService.Publish(new BattleTurnInited(_ctx.ActiveUnit, _currentActionId));
        _sm.Fire(BattleRoundsTrigger.NextTurn);
    }

    private void OnTurnStart()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.TurnStart);
        BattleLogger.LogActiveUnit(_ctx.ActiveUnit);

        _ctx.BattleSceneUIController.RenderAbilityList(
            _ctx.ActiveUnit.Abilities,
            _ctx.BattleAbilitiesManager,
            _ctx.ActiveUnit
        );

        _sm.Fire(BattleRoundsTrigger.NextTurn);
    }

    private void OnWaitTurnAction()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.TurnWaitAction);
        IBattleActionController controller = _actionControllerResolver.ResolveFor(_ctx.ActiveUnit);

        controller.RequestAction(_ctx, action =>
        {
            if (action == null)
            {
                Debug.LogWarning($"[{nameof(BattleRoundsMachine)}.{nameof(OnWaitTurnAction)}] Battle action controller returned no action. Skipping turn.");
                _sm.Fire(BattleRoundsTrigger.SkipTurn);
                return;
            }

            try
            {
                AttachAction(action);
                action.Resolve();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(BattleRoundsMachine)}.{nameof(OnWaitTurnAction)}] Unexpected exception while resolving action: {exception}");
                DetachCurrentAction();
                _sm.Fire(BattleRoundsTrigger.SkipTurn);
            }
        });
    }

    private void OnTurnSkip()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.TurnSkip);
        _sm.Fire(BattleRoundsTrigger.NextTurn);
    }

    private void OnTurnEnd()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.TurnEnd);
        _ctx.SceneEventBusService.Publish(new BattleTurnEnded(_ctx.ActiveUnit, _currentActionId));
        _ctx.ActiveUnit = null;

        if (BattleResult.CheckForBattleCompletion(_battleFinished, _ctx.BattleUnits))
        {
            FinishBattle();
            return;
        }

        _sm.Fire(BattleRoundsTrigger.NextTurn);
    }

    private void OnRoundEnd()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.RoundEnd);
        if (_battleFinished)
            return;
        _sm.Fire(BattleRoundsTrigger.StartNewRound);
    }

    private void AttachAction(IBattleAction action)
    {
        DetachCurrentAction();

        _ctx.CurrentAction = action ?? throw new ArgumentNullException(nameof(action));
        action.OnResolve += OnActionResolved;
        action.OnCancel += OnActionCancelled;

        _ctx.SceneEventBusService.Publish(new BattleActionSelected(action, _ctx.ActiveUnit, _currentActionId));
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

        _ctx.CurrentAction = null;
    }

    private void OnActionResolved()
    {
        var resolvedAction = _ctx.CurrentAction;
        var activeUnit = _ctx.ActiveUnit;

        _ctx.SceneEventBusService.Publish(new BattleActionResolved(resolvedAction, activeUnit, _currentActionId));
        DetachCurrentAction();

        if (resolvedAction != null && activeUnit != null)
        {
            BattleLogger.LogUnitAction(activeUnit, BattleLogger.ResolveActionName(resolvedAction));
        }

        switch (resolvedAction)
        {
            case BattleActionDefend:
                _sm.Fire(BattleRoundsTrigger.SkipTurn);
                break;
            case BattleActionSkipTurn:
                _sm.Fire(BattleRoundsTrigger.SkipTurn);
                break;
            case BattleActionAttack:
                _sm.Fire(BattleRoundsTrigger.ActionDone);
                break;
            case BattleActionAbility:
                _sm.Fire(BattleRoundsTrigger.ActionDone);
                break;
            default:
                _sm.Fire(BattleRoundsTrigger.ActionDone);
                break;
        }
    }

    private void OnActionCancelled()
    {
        var activeUnit = _ctx.ActiveUnit;
        _ctx.SceneEventBusService.Publish(new BattleActionCancelled(activeUnit, _currentActionId));
        DetachCurrentAction();

        if (activeUnit == null || !activeUnit.IsFriendly())
            return;

        OnWaitTurnAction();
    }

    private void FinishBattle()
    {
        if (_battleFinished)
            return;

        _battleFinished = true;

        UnsubscribeFromGameEvents();

        if (_sm.CanFire(BattleRoundsTrigger.EndRound))
        {
            _sm.Fire(BattleRoundsTrigger.EndRound);
        }

        TrackKnownSquads(_ctx.BattleUnits);
        _battleResult = new BattleResult(
            _playerRequestedFlee,
            _friendlySquadHistory,
            _enemySquadHistory,
            _initialSquadCounts);

        _ctx.SceneEventBusService.Publish(new BattleFinished(_battleResult));
    }

    private void SubscribeToGameEvents()
    {
        _ctx.SceneEventBusService.Subscribe<RequestFleeCombat>(HandleLeaveCombat);
    }

    private void UnsubscribeFromGameEvents()
    {
        _ctx.SceneEventBusService.Unsubscribe<RequestFleeCombat>(HandleLeaveCombat);
    }

    private void HandleLeaveCombat(RequestFleeCombat evt)
    {
        _playerRequestedFlee = true;
        FinishBattle();
    }

    private void InitializeSquadHistory()
    {
        _friendlySquadHistory.Clear();
        _enemySquadHistory.Clear();
        _friendlySquadSet.Clear();
        _enemySquadSet.Clear();
        _initialSquadCounts.Clear();
        TrackKnownSquads(_ctx.BattleUnits);
    }

    private void TrackKnownSquads(IEnumerable<BattleSquadController> squads)
    {
        if (squads == null)
            return;

        foreach (var squadController in squads)
        {
            if (squadController == null)
                continue;

            var model = squadController.GetSquadModel();
            TrackKnownSquadModel(model);
        }
    }

    private void TrackKnownSquadModel(IReadOnlySquadModel model)
    {
        if (model == null)
            return;

        if (!_initialSquadCounts.ContainsKey(model))
            _initialSquadCounts[model] = Mathf.Max(0, model.Count);

        if (model.IsFriendly() || model.IsAlly() || model.IsHero())
        {
            if (_friendlySquadSet.Add(model))
                _friendlySquadHistory.Add(model);
            return;
        }

        if (model.IsEnemy())
        {
            if (_enemySquadSet.Add(model))
                _enemySquadHistory.Add(model);
        }
    }
}
