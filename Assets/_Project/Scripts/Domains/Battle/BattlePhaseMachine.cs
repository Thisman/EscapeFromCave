using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BattlePhaseMachine
{
    private readonly StateMachine<BattlePhase, BattleTrigger> _sm;
    private readonly IBattleContext _ctx;
    private readonly BattleRoundsMachine _battleRoundsMachine;

    public BattlePhaseMachine(IBattleContext ctx, BattleRoundsMachine battleRoundsMachine)
    {
        _ctx = ctx;
        _battleRoundsMachine = battleRoundsMachine;
        _sm = new StateMachine<BattlePhase, BattleTrigger>(BattlePhase.Loading);

        if (_battleRoundsMachine != null)
        {
            _battleRoundsMachine.BattleFinished += HandleBattleFinished;
        }

        _sm.Configure(BattlePhase.Loading)
            .Permit(BattleTrigger.Start, BattlePhase.Tactics);

        _sm.Configure(BattlePhase.Tactics)
            .OnEntry(() => OnEnterTactics())
            .OnExit(() => OnExitTactics())
            .Permit(BattleTrigger.EndTactics, BattlePhase.BattleRounds);

        _sm.Configure(BattlePhase.BattleRounds)
            .OnEntry(() => OnEnterRounds())
            .Permit(BattleTrigger.EndRounds, BattlePhase.Results);

        _sm.Configure(BattlePhase.Results)
            .OnEntry(() => OnEnterResults())
            .Ignore(BattleTrigger.ForceResults); // финал
    }

    public BattlePhase State => _sm.State;

    public void Fire(BattleTrigger trigger)
    {
        if (_sm.CanFire(trigger)) _sm.Fire(trigger);
    }

    private void OnEnterTactics()
    {
        _ctx.PanelManager?.Show("tactic");
        _ctx.BattleGridDragAndDropController.enabled = true;

        var units = _ctx.BattleUnits;
        if (_ctx.BattleGridController == null || units == null || units.Count == 0)
            return;

        bool requiresPlacement = false;

        foreach (var unit in units)
        {
            if (unit == null)
            {
                requiresPlacement = true;
                break;
            }

            if (!_ctx.BattleGridController.TryGetSlotForOccupant(unit.transform, out _))
            {
                requiresPlacement = true;
                break;
            }
        }

        if (!requiresPlacement)
            return;

        if (!_ctx.BattleGridController.TryPlaceUnits(units))
        {
            Debug.LogWarning("Failed to place battle units on the grid during tactics phase.");
        }
    }

    private void OnEnterRounds()
    {
        _ctx.PanelManager?.Show("rounds");
        _battleRoundsMachine.Reset();
        _battleRoundsMachine.BeginRound();
    }

    private void OnEnterResults()
    {
        _ctx.PanelManager?.Show("results");
        _ctx.IsFinished = true;
        // сериализация результатов, подсчёт лута/опыта
    }

    private void HandleBattleFinished()
    {
        Fire(BattleTrigger.EndRounds);
    }

    private void OnExitTactics()
    {
        _ctx.BattleGridController.DisableSlots();
        _ctx.BattleGridDragAndDropController.enabled = false;
    }
}
