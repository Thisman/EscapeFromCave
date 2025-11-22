using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TargetHighlightSystem : IDisposable
{
    private readonly BattleContext _ctx;
    private readonly List<IDisposable> _subscriptions = new();

    public TargetHighlightSystem(BattleContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        var bus = _ctx.SceneEventBusService ?? throw new ArgumentNullException(nameof(_ctx.SceneEventBusService));

        _subscriptions.Add(bus.Subscribe<RoundStartedEvent>(HandleRoundStarted));
        _subscriptions.Add(bus.Subscribe<TurnPreparedEvent>(HandleTurnPrepared));
        _subscriptions.Add(bus.Subscribe<TurnEndedEvent>(HandleTurnEnded));
        _subscriptions.Add(bus.Subscribe<ActionSelectedEvent>(HandleActionSelected));
        _subscriptions.Add(bus.Subscribe<ActionCancelledEvent>(HandleActionCancelled));
        _subscriptions.Add(bus.Subscribe<BattleFinishedEvent>(_ => ClearAllHighlights()));
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }

        _subscriptions.Clear();
    }

    private void HandleRoundStarted(RoundStartedEvent evt)
    {
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);
        _ctx.BattleSceneUIController.ResetAbilityHighlight();
    }

    private void HandleTurnPrepared(TurnPreparedEvent evt)
    {
        HighlightActiveUnitSlot(evt.ActiveUnit);
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);
    }

    private void HandleTurnEnded(TurnEndedEvent evt)
    {
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);
        _ctx.BattleSceneUIController.ResetAbilityHighlight();
    }

    private void HandleActionSelected(ActionSelectedEvent evt)
    {
        if (evt.Action is BattleActionAbility abilityAction)
        {
            _ctx.BattleSceneUIController.HighlightAbility(abilityAction.Ability);
        }
        else
        {
            _ctx.BattleSceneUIController.ResetAbilityHighlight();
        }

        ApplyActionSlotHighlights(evt.Action, evt.Actor);
    }

    private void HandleActionCancelled(ActionCancelledEvent evt)
    {
        _ctx.BattleSceneUIController.ResetAbilityHighlight();
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);
    }

    private void HighlightActiveUnitSlot(IReadOnlySquadModel activeUnit)
    {
        if (activeUnit == null)
            return;

        if (!_ctx.TryGetSquadController(activeUnit, out var controller) || controller == null)
            return;

        var gridController = _ctx.BattleGridController;
        if (gridController == null)
            return;

        gridController.ClearActiveSlot();

        if (!gridController.TryGetSlotForOccupant(controller.transform, out var slot) || slot == null)
            return;

        gridController.SetActiveSlot(slot);
    }

    private void ClearActionSlotHighlights()
    {
        var gridController = _ctx.BattleGridController;
        if (gridController == null)
            return;

        gridController.ResetAllSlotHighlights(keepActiveHighlight: false);
        HighlightActiveUnitSlot(_ctx.ActiveUnit);
    }

    private void ApplyActionSlotHighlights(IBattleAction action, IReadOnlySquadModel actor)
    {
        ClearActionSlotHighlights();
        UpdateTargetValidity(null, null);

        if (action == null || actor == null)
            return;

        if (!actor.IsFriendly())
            return;

        if (action is not IBattleActionTargetResolverProvider resolverProvider)
            return;

        var targetResolver = resolverProvider.TargetResolver;
        if (targetResolver == null)
            return;

        UpdateTargetValidity(targetResolver, actor);

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
                        UnityEngine.Debug.LogError($"[{nameof(TargetHighlightSystem)}.{nameof(UpdateTargetValidity)}] Failed to resolve target: {exception}");
                    }
                }
            }

            unitController.SetTargetValidity(isValid);
        }
    }

    private void ClearAllHighlights()
    {
        ClearActionSlotHighlights();
        _ctx.BattleGridController.ClearActiveSlot();
        _ctx.BattleSceneUIController.ResetAbilityHighlight();
    }
}
