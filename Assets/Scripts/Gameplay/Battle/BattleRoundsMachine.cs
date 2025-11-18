using System;
using System.Collections.Generic;
using System.Linq;
using Stateless;
using UnityEngine;

public sealed class BattleRoundsMachine
{
    private bool _battleFinished;
    private bool _playerRequestedFlee;
    private BattleResult _battleResult;
    private readonly BattleContext _ctx;
    private readonly StateMachine<BattleRoundStates, BattleRoundsTrigger> _sm;

    private readonly HashSet<IReadOnlySquadModel> _enemySquadSet = new();
    private readonly List<IReadOnlySquadModel> _enemySquadHistory = new();
    private readonly List<BattleSquadModel> _subscribedSquadModels = new();
    private readonly HashSet<IReadOnlySquadModel> _friendlySquadSet = new();
    private readonly List<IReadOnlySquadModel> _friendlySquadHistory = new();
    private readonly Dictionary<IReadOnlySquadModel, int> _initialSquadCounts = new();
    private readonly PlayerBattleActionController _playerTurnController;
    private readonly AIBattleActionController _enemyTurnController;
    private readonly ProviderForBattleActionController _actionControllerResolver;

    public event Action<BattleResult> OnBattleRoundsFinished;

    public BattleRoundsMachine(BattleContext ctx)
    {
        _ctx = ctx;
        _sm = new StateMachine<BattleRoundStates, BattleRoundsTrigger>(BattleRoundStates.RoundInit);

        _sm.Configure(BattleRoundStates.RoundInit)
            .OnEntry(OnRoundInit)
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
            .Permit(BattleRoundsTrigger.StartNewRound, BattleRoundStates.RoundInit);

        _playerTurnController = new PlayerBattleActionController();
        _enemyTurnController = new AIBattleActionController();
        _actionControllerResolver = new ProviderForBattleActionController(_playerTurnController, _enemyTurnController);
    }

    public void Reset()
    {
        UnsubscribeFromSquadEvents();
        _battleFinished = false;
        _playerRequestedFlee = false;
        _battleResult = null;
        _ctx.BattleSceneUIController.OnLeaveCombat += HandleLeaveCombat;
        InitializeSquadHistory();
        UpdateTargetValidity(null, null);
        SubscribeToSquadEvents();
    }

    public void BeginRounds() => OnRoundInit();

    private void OnRoundInit()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.RoundInit);
        _ctx.DefendedUnitsThisRound?.Clear();

        var unitModels = _ctx.BattleUnits
                .Where(unit => unit != null)
                .Select(unit => unit.GetSquadModel())
                .Where(model => model != null);

        _ctx.BattleQueueController.Build(unitModels);
        _ctx.BattleSceneUIController.RenderQueue(_ctx.BattleQueueController);
        _ctx.BattleAbilitiesManager.OnTick();
        TriggerEffects(BattleEffectTrigger.OnTurnStart);
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

        _ctx.BattleSceneUIController.RenderQueue(_ctx.BattleQueueController);
        _sm.Fire(BattleRoundsTrigger.NextTurn);
    }

    private void OnTurnStart()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.TurnStart);

        var queue = _ctx.BattleQueueController.GetQueue();
        _ctx.ActiveUnit = queue[0];
        HighlightActiveUnitSlot();
        BattleLogger.LogActiveUnit(_ctx.ActiveUnit);

        TriggerEffects(BattleEffectTrigger.OnTurnStart);
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
        ClearActionSlotHighlights();
        ClearActiveUnitSlotHighlight();

        TriggerEffects(BattleEffectTrigger.OnTurnEnd, _ctx.ActiveUnit);

        _ctx.BattleQueueController.NextTurn();
        _ctx.ActiveUnit = null;

        RemoveDefeatedUnits(_ctx.BattleQueueController, _ctx.BattleGridController);

        if (BattleResult.CheckForBattleCompletion(_battleFinished, _ctx.BattleUnits))
        {
            TriggerBattleFinish();
            return;
        }

        _sm.Fire(BattleRoundsTrigger.NextTurn);
    }

    // TODO: перенести в BattleGridController
    private void HighlightActiveUnitSlot()
    {
        var activeUnitModel = _ctx.ActiveUnit;
        if (activeUnitModel == null)
            return;

        if (!_ctx.TryGetSquadController(activeUnitModel, out var controller) || controller == null)
            return;

        var gridController = _ctx.BattleGridController;
        if (gridController == null)
            return;

        gridController.ClearActiveSlot();

        if (!gridController.TryGetSlotForOccupant(controller.transform, out var slot) || slot == null)
            return;

        gridController.SetActiveSlot(slot);
    }

    // TODO: перенести в BattleGridController
    private void ClearActiveUnitSlotHighlight()
    {
        _ctx.BattleGridController.ClearActiveSlot();
    }

    private void OnRoundEnd()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundStates.RoundEnd);
        if (_battleFinished)
            return;

        TriggerEffects(BattleEffectTrigger.OnRoundEnd);
        _sm.Fire(BattleRoundsTrigger.StartNewRound);
    }

    private void AttachAction(IBattleAction action)
    {
        DetachCurrentAction();

        _ctx.CurrentAction = action ?? throw new ArgumentNullException(nameof(action));
        action.OnResolve += OnActionResolved;
        action.OnCancel += OnActionCancelled;

        if (action is BattleActionAbility abilityAction)
        {
            _ctx.BattleSceneUIController.HighlightAbility(abilityAction.Ability);
        }
        else
        {
            _ctx.BattleSceneUIController.ResetAbilityHighlight();
        }

        ApplyActionSlotHighlights(action);
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

        _ctx.BattleSceneUIController.ResetAbilityHighlight();
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);

        _ctx.CurrentAction = null;
    }

    private void OnActionResolved()
    {
        var resolvedAction = _ctx.CurrentAction;
        var activeUnit = _ctx.ActiveUnit;

        DetachCurrentAction();

        if (resolvedAction != null && activeUnit != null)
        {
            BattleLogger.LogUnitAction(activeUnit, BattleLogger.ResolveActionName(resolvedAction));
            TriggerEffects(BattleEffectTrigger.OnAction, activeUnit);
        }

        switch (resolvedAction)
        {
            case BattleActionDefend:
                TriggerEffects(BattleEffectTrigger.OnDefend, activeUnit);
                _ctx.DefendedUnitsThisRound.Add(_ctx.ActiveUnit);
                _ctx.BattleQueueController.AddLast(_ctx.ActiveUnit);
                _sm.Fire(BattleRoundsTrigger.SkipTurn);
                break;
            case BattleActionSkipTurn:
                TriggerEffects(BattleEffectTrigger.OnSkip, activeUnit);
                _sm.Fire(BattleRoundsTrigger.SkipTurn);
                break;
            case BattleActionAttack:
                TriggerEffects(BattleEffectTrigger.OnAttack, activeUnit);
                _sm.Fire(BattleRoundsTrigger.ActionDone);
                break;
            case BattleActionAbility:
                TriggerEffects(BattleEffectTrigger.OnAbility, activeUnit);
                _sm.Fire(BattleRoundsTrigger.ActionDone);
                break;
            default:
                _sm.Fire(BattleRoundsTrigger.ActionDone);
                break;
        }
    }

    private void OnActionCancelled()
    {
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);

        if (!_ctx.ActiveUnit.IsFriendly())
            return;

        _ctx.BattleSceneUIController.ResetAbilityHighlight();

        OnWaitTurnAction();
    }

    private void TriggerBattleFinish()
    {
        if (_battleFinished)
            return;

        _battleFinished = true;

        UnsubscribeFromSquadEvents();

        _ctx.BattleSceneUIController.OnLeaveCombat -= HandleLeaveCombat;
        _ctx.BattleQueueController.Build(Array.Empty<IReadOnlySquadModel>());

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

        OnBattleRoundsFinished?.Invoke(_battleResult);
    }

    private void RemoveDefeatedUnits(BattleQueueController queueController, BattleGridController gridController)
    {
        TrackKnownSquads(_ctx.BattleUnits);

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
                continue;
            }

            defeatedUnits.Add(unitController);
        }

        if (defeatedUnits.Count == 0)
            return;

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

    // TODO: пеенести в BattleGridController
    private void ApplyActionSlotHighlights(IBattleAction action)
    {
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);

        var activeUnit = _ctx.ActiveUnit;
        if (action == null || activeUnit == null)
            return;

        if (!activeUnit.IsFriendly())
            return;

        if (action is not IBattleActionTargetResolverProvider resolverProvider)
            return;

        var targetResolver = resolverProvider.TargetResolver;
        if (targetResolver == null)
            return;

        UpdateTargetValidity(targetResolver, activeUnit);

        var gridController = _ctx.BattleGridController;
        if (gridController == null)
            return;

        var units = _ctx.BattleUnits;
        if (units == null)
            return;

        var availableSlots = new List<Transform>();

        foreach (var unitController in units)
        {
            if (unitController == null || !unitController.IsValidTarget())
                continue;

            if (!gridController.TryGetSlotForOccupant(unitController.transform, out var slot) || slot == null)
                continue;

            availableSlots.Add(slot);
        }

        gridController.HighlightSlots(availableSlots, BattleGridSlotHighlightMode.Available);
    }

    // TODO: пеенести в BattleGridController
    private void ClearActionSlotHighlights()
    {
        var gridController = _ctx.BattleGridController;
        if (gridController == null)
            return;

        gridController.ResetAllSlotHighlights(keepActiveHighlight: false);
        HighlightActiveUnitSlot();
    }

    // TODO: перенести в BattleSquadController
    private void UpdateTargetValidity(IBattleActionTargetResolver targetResolver, IReadOnlySquadModel actor)
    {
        var units = _ctx.BattleUnits;
        if (units == null)
            return;

        foreach (var unitController in units)
        {
            if (unitController == null)
                continue;

            bool isValid = false;

            if (targetResolver != null && actor != null)
            {
                var targetModel = unitController.GetSquadModel();

                if (targetModel != null)
                {
                    try
                    {
                        isValid = targetResolver.ResolveTarget(actor, targetModel);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[{nameof(BattleRoundsMachine)}.{nameof(UpdateTargetValidity)}] Failed to resolve target: {exception}");
                    }
                }
            }

            unitController.SetTargetValidity(isValid);
        }
    }

    // TODO: Перенести в менеджеры эффектов и способностей
    private void SubscribeToSquadEvents()
    {
        var units = _ctx.BattleUnits;
        if (units == null)
            return;

        foreach (var unitController in units)
        {
            if (unitController == null)
                continue;

            if (unitController.GetSquadModel() is not BattleSquadModel model)
                continue;

            if (_subscribedSquadModels.Contains(model))
                continue;

            model.OnEvent += HandleSquadEvent;
            _subscribedSquadModels.Add(model);
        }
    }

    // TODO: Перенести в менеджеры эффектов и способностей
    private void UnsubscribeFromSquadEvents()
    {
        if (_subscribedSquadModels.Count == 0)
            return;

        foreach (var model in _subscribedSquadModels)
        {
            if (model != null)
                model.OnEvent -= HandleSquadEvent;
        }

        _subscribedSquadModels.Clear();
    }

    // TODO: Перенести в менеджеры эффектов и способностей
    private void HandleSquadEvent(BattleSquadEvent squadEvent)
    {
        if (squadEvent.Type != BattleSquadEventType.ApplyDamage)
            return;

        if (squadEvent.Squad == null || squadEvent.AppliedDamage <= 0)
            return;

        TriggerEffects(BattleEffectTrigger.OnApplyDamage, squadEvent.Squad);

        var actingUnit = _ctx.ActiveUnit;
        if (actingUnit != null)
        {
            TriggerEffects(BattleEffectTrigger.OnDealDamage, actingUnit);
        }
    }

    // TODO: Перенести в менеджеры эффектов и способностей
    private async void TriggerEffects(BattleEffectTrigger trigger)
    {
        await _ctx.BattleEffectsManager.Trigger(trigger);
    }

    // TODO: Перенести в менеджеры эффектов и способностей
    private async void TriggerEffects(BattleEffectTrigger trigger, IReadOnlySquadModel unit)
    {
        if (unit == null)
            return;

        if (!_ctx.TryGetSquadController(unit, out var controller) || controller == null)
            return;

        if (!controller.TryGetComponent<BattleSquadEffectsController>(out var effectsController))
            return;

        await _ctx.BattleEffectsManager.Trigger(trigger, effectsController);
    }

    private void HandleLeaveCombat()
    {
        _playerRequestedFlee = true;
        TriggerBattleFinish();
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
