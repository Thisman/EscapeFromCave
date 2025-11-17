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
    private readonly List<BattleSquadModel> _subscribedSquadModels = new();
    private readonly List<IReadOnlySquadModel> _friendlySquadHistory = new();
    private readonly List<IReadOnlySquadModel> _enemySquadHistory = new();
    private readonly HashSet<IReadOnlySquadModel> _friendlySquadSet = new();
    private readonly HashSet<IReadOnlySquadModel> _enemySquadSet = new();

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
        UnsubscribeFromSquadEvents();
        _battleFinished = false;
        _playerRequestedFlee = false;
        if (_ctx.BattleUIController != null)
            _ctx.BattleUIController.OnLeaveCombat += HandleLeaveCombat;
        InitializeSquadHistory();
        UpdateTargetValidity(null, null);
        SubscribeToSquadEvents();
    }

    public void BeginRounds() => OnRoundInit();

    private void OnRoundInit()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundState.RoundInit);
        _ctx.DefendedUnitsThisRound?.Clear();

        var unitModels = _ctx.BattleUnits
                .Where(unit => unit != null)
                .Select(unit => unit.GetSquadModel())
                .Where(model => model != null);

        _ctx.BattleQueueController.Build(unitModels);
        _ctx.BattleUIController?.RenderQueue(_ctx.BattleQueueController);
        _ctx.BattleAbilitiesManager.OnTick();
        TriggerEffects(BattleEffectTrigger.OnTurnStart);
        _sm.Fire(BattleRoundTrigger.InitTurn);
    }

    private void OnTurnInit()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundState.TurnInit);
        var queue = _ctx.BattleQueueController.GetQueue();
        if (queue.Count == 0)
        {
            _ctx.ActiveUnit = null;
            _sm.Fire(BattleRoundTrigger.EndRound);
            return;
        }

        _ctx.BattleUIController?.RenderQueue(_ctx.BattleQueueController);
        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnTurnStart()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundState.TurnStart);
        var queue = _ctx.BattleQueueController.GetQueue();
        _ctx.ActiveUnit = queue[0];
        BattleLogger.LogActiveUnit(_ctx.ActiveUnit);

        HighlightActiveUnitSlot();

        var abilities = _ctx.ActiveUnit.Abilities;
        var activeUnit = _ctx.ActiveUnit;
        var abilityManager = _ctx.BattleAbilitiesManager;
        TriggerEffects(BattleEffectTrigger.OnTurnStart);
        _ctx.BattleUIController?.RenderAbilityList(abilities, abilityManager, activeUnit);

        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnWaitTurnAction()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundState.TurnWaitAction);
        IReadOnlySquadModel activeUnit = _ctx.ActiveUnit;
        BattleActionControllerResolver resolver = _ctx.BattleActionControllerResolver;
        IBattleActionController controller = resolver.ResolveFor(activeUnit);

        controller.RequestAction(_ctx, action =>
        {
            if (action == null)
            {
                Debug.LogWarning($"[{nameof(BattleRoundsMachine)}.{nameof(OnWaitTurnAction)}] Battle action controller returned no action. Skipping turn.");
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
                Debug.LogError($"[{nameof(BattleRoundsMachine)}.{nameof(OnWaitTurnAction)}] Unexpected exception while resolving action: {exception}");
                DetachCurrentAction();
                _sm.Fire(BattleRoundTrigger.SkipTurn);
            }
        });
    }

    private void OnTurnSkip()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundState.TurnSkip);
        _sm.Fire(BattleRoundTrigger.NextTurn);
    }

    private void OnTurnEnd()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundState.TurnEnd);
        ClearActionSlotHighlights();
        ClearActiveUnitSlotHighlight();

        TriggerEffects(BattleEffectTrigger.OnTurnEnd, _ctx.ActiveUnit);

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

    private void HighlightActiveUnitSlot()
    {
        var activeUnitModel = _ctx.ActiveUnit;
        if (activeUnitModel == null)
            return;

        if (!_ctx.TryGetController(activeUnitModel, out var controller) || controller == null)
            return;

        var gridController = _ctx.BattleGridController;
        if (gridController == null)
            return;

        gridController.ClearActiveSlot();

        if (!gridController.TryGetSlotForOccupant(controller.transform, out var slot) || slot == null)
            return;

        gridController.SetActiveSlot(slot);
    }

    private void ClearActiveUnitSlotHighlight()
    {
        var gridController = _ctx.BattleGridController;
        gridController?.ClearActiveSlot();
    }

    private void OnRoundEnd()
    {
        BattleLogger.LogRoundStateEntered(BattleRoundState.RoundEnd);
        if (_battleFinished)
            return;

        TriggerEffects(BattleEffectTrigger.OnRoundEnd);
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

        if (action is BattleActionAbility abilityAction)
        {
            _ctx.BattleUIController?.HighlightAbility(abilityAction.Ability);
        }
        else
        {
            _ctx.BattleUIController?.ResetAbilityHighlight();
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

        _ctx.BattleUIController?.ResetAbilityHighlight();
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
            BattleLogger.LogUnitAction(activeUnit, ResolveActionName(resolvedAction));
            TriggerEffects(BattleEffectTrigger.OnAction, activeUnit);
        }

        switch (resolvedAction)
        {
            case BattleActionDefend:
                TriggerEffects(BattleEffectTrigger.OnDefend, activeUnit);
                _ctx.DefendedUnitsThisRound.Add(_ctx.ActiveUnit);
                _ctx.BattleQueueController.AddLast(_ctx.ActiveUnit);
                _sm.Fire(BattleRoundTrigger.SkipTurn);
                break;
            case BattleActionSkipTurn:
                TriggerEffects(BattleEffectTrigger.OnSkip, activeUnit);
                _sm.Fire(BattleRoundTrigger.SkipTurn);
                break;
            case BattleActionAttack:
                TriggerEffects(BattleEffectTrigger.OnAttack, activeUnit);
                _sm.Fire(BattleRoundTrigger.ActionDone);
                break;
            case BattleActionAbility:
                TriggerEffects(BattleEffectTrigger.OnAbility, activeUnit);
                _sm.Fire(BattleRoundTrigger.ActionDone);
                break;
            default:
                _sm.Fire(BattleRoundTrigger.ActionDone);
                break;
        }
    }

    private void OnActionCancelled()
    {
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);

        if (!_ctx.ActiveUnit.IsFriendly())
            return;

        _ctx.BattleUIController?.ResetAbilityHighlight();

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

        UnsubscribeFromSquadEvents();

        if (_ctx.BattleUIController != null)
            _ctx.BattleUIController.OnLeaveCombat -= HandleLeaveCombat;
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

    private void ClearActionSlotHighlights()
    {
        var gridController = _ctx.BattleGridController;
        if (gridController == null)
            return;

        gridController.ResetAllSlotHighlights(keepActiveHighlight: false);
        HighlightActiveUnitSlot();
    }

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

    private async void TriggerEffects(BattleEffectTrigger trigger)
    {
        var effectsManager = _ctx?.BattleEffectsManager;
        if (effectsManager == null)
            return;

        await effectsManager.Trigger(trigger);
    }

    private async void TriggerEffects(BattleEffectTrigger trigger, IReadOnlySquadModel unit)
    {
        if (unit == null)
            return;

        var effectsManager = _ctx?.BattleEffectsManager;
        if (effectsManager == null)
            return;

        if (!_ctx.TryGetController(unit, out var controller) || controller == null)
            return;

        var effectsController = controller.GetComponent<BattleSquadEffectsController>();
        if (effectsController == null)
            return;

        await effectsManager.Trigger(trigger, effectsController);
    }

    private void HandleLeaveCombat()
    {
        _playerRequestedFlee = true;
        TriggerBattleFinish();
    }

    private static string ResolveActionName(IBattleAction action)
    {
        switch (action)
        {
            case BattleActionAttack:
                return "Attack";
            case BattleActionDefend:
                return "Defend";
            case BattleActionSkipTurn:
                return "Skip Turn";
            case BattleActionAbility abilityAction:
                var ability = abilityAction.Ability;
                if (ability == null)
                    return "Ability";
                return string.IsNullOrWhiteSpace(ability.AbilityName)
                    ? (!string.IsNullOrWhiteSpace(ability.name) ? ability.name : "Ability")
                    : ability.AbilityName;
            default:
                return action?.GetType().Name ?? "Unknown";
        }
    }

    private BattleUnitsResult BuildUnitsResult()
    {
        TrackKnownSquads(_ctx.BattleUnits);

        IReadOnlySquadModel[] friendlyUnits = _friendlySquadHistory.Count > 0
            ? _friendlySquadHistory.ToArray()
            : Array.Empty<IReadOnlySquadModel>();

        IReadOnlySquadModel[] enemyUnits = _enemySquadHistory.Count > 0
            ? _enemySquadHistory.ToArray()
            : Array.Empty<IReadOnlySquadModel>();

        return new BattleUnitsResult(friendlyUnits, enemyUnits);
    }

    private void InitializeSquadHistory()
    {
        _friendlySquadHistory.Clear();
        _enemySquadHistory.Clear();
        _friendlySquadSet.Clear();
        _enemySquadSet.Clear();
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

    private BattleResultStatus DetermineBattleStatus(BattleUnitsResult unitsResult)
    {
        if (_playerRequestedFlee)
            return BattleResultStatus.Flee;

        bool heroAlive = unitsResult.FriendlyUnits.Any(model => model.IsHero() && model.Count > 0);
        bool hasAliveFriendlies = unitsResult.FriendlyUnits.Any(model => model.Count > 0);
        bool hasAliveEnemies = unitsResult.EnemyUnits.Any(model => model.Count > 0);

        if (!heroAlive)
            return BattleResultStatus.Defeat;

        if (hasAliveFriendlies && !hasAliveEnemies)
            return BattleResultStatus.Victory;

        if (hasAliveFriendlies)
            return BattleResultStatus.Victory;

        return BattleResultStatus.Defeat;
    }
}
