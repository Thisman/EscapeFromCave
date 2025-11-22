using System;
using System.Collections.Generic;

public sealed class BattleEffectTriggerSystem : IDisposable
{
    private readonly BattleContext _ctx;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly List<BattleSquadModel> _subscribedSquadModels = new();

    public BattleEffectTriggerSystem(BattleContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        var bus = _ctx.SceneEventBusService ?? throw new ArgumentNullException(nameof(_ctx.SceneEventBusService));

        _subscriptions.Add(bus.Subscribe<BattleRoundStarted>(HandleRoundStarted));
        _subscriptions.Add(bus.Subscribe<BattleTurnInited>(HandleTurnInited));
        _subscriptions.Add(bus.Subscribe<BattleTurnEnded>(HandleTurnEnded));
        _subscriptions.Add(bus.Subscribe<BattleActionResolved>(HandleActionResolved));
        _subscriptions.Add(bus.Subscribe<BattleFinished>(_ => UnsubscribeFromSquadEvents()));

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

    private void HandleRoundStarted(BattleRoundStarted evt)
    {
        _ctx.DefendedUnitsThisRound?.Clear();
        TriggerEffects(BattleEffectTrigger.OnRoundStart);
    }

    private void HandleTurnInited(BattleTurnInited evt)
    {
        TriggerEffects(BattleEffectTrigger.OnTurnStart, evt.ActiveUnit);
    }

    private void HandleTurnEnded(BattleTurnEnded evt)
    {
        TriggerEffects(BattleEffectTrigger.OnTurnEnd, evt.ActiveUnit);
    }

    private void HandleActionResolved(BattleActionResolved evt)
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
