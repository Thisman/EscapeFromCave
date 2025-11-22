using System;
using System.Collections.Generic;

public sealed class EffectTriggerSystem : IDisposable
{
    private readonly BattleContext _ctx;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly List<BattleSquadModel> _subscribedSquadModels = new();

    public EffectTriggerSystem(BattleContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        var bus = _ctx.SceneEventBusService ?? throw new ArgumentNullException(nameof(_ctx.SceneEventBusService));

        _subscriptions.Add(bus.Subscribe<RoundStartedEvent>(HandleRoundStarted));
        _subscriptions.Add(bus.Subscribe<TurnPreparedEvent>(HandleTurnPrepared));
        _subscriptions.Add(bus.Subscribe<TurnEndedEvent>(HandleTurnEnded));
        _subscriptions.Add(bus.Subscribe<ActionResolvedEvent>(HandleActionResolved));
        _subscriptions.Add(bus.Subscribe<BattleFinishedEvent>(_ => UnsubscribeFromSquadEvents()));

        SubscribeToSquadEvents();
    }

    public void Dispose()
    {
        UnsubscribeFromSquadEvents();

        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }

        _subscriptions.Clear();
    }

    private void HandleRoundStarted(RoundStartedEvent evt)
    {
        _ctx.DefendedUnitsThisRound?.Clear();
        TriggerEffects(BattleEffectTrigger.OnRoundStart);
    }

    private void HandleTurnPrepared(TurnPreparedEvent evt)
    {
        TriggerEffects(BattleEffectTrigger.OnTurnStart, evt.ActiveUnit);
    }

    private void HandleTurnEnded(TurnEndedEvent evt)
    {
        TriggerEffects(BattleEffectTrigger.OnTurnEnd, evt.ActiveUnit);
    }

    private void HandleActionResolved(ActionResolvedEvent evt)
    {
        switch (evt.Action)
        {
            case BattleActionDefend:
                TriggerEffects(BattleEffectTrigger.OnDefend, evt.Actor);
                _ctx.DefendedUnitsThisRound.Add(evt.Actor);
                _ctx.BattleQueueController.AddLast(evt.Actor);
                break;
            case BattleActionSkipTurn:
                TriggerEffects(BattleEffectTrigger.OnSkip, evt.Actor);
                break;
            case BattleActionAttack:
                TriggerEffects(BattleEffectTrigger.OnAttack, evt.Actor);
                TriggerEffects(BattleEffectTrigger.OnAction, evt.Actor);
                break;
            case BattleActionAbility:
                TriggerEffects(BattleEffectTrigger.OnAbility, evt.Actor);
                TriggerEffects(BattleEffectTrigger.OnAction, evt.Actor);
                break;
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
        await _ctx.BattleEffectsManager.Trigger(trigger);
    }

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
}
