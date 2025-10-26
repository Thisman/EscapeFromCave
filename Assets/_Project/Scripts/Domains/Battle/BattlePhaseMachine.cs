using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BattlePhaseMachine
{
    private readonly BattleContext _ctx;
    private readonly StateMachine<BattlePhase, BattleTrigger> _sm;
    private readonly BattleRoundsMachine _battleRoundsMachine;

    public BattlePhaseMachine(BattleContext ctx, BattleRoundsMachine battleRoundsMachine)
    {
        _ctx = ctx;
        _battleRoundsMachine = battleRoundsMachine;
        _sm = new StateMachine<BattlePhase, BattleTrigger>(BattlePhase.Loading);

        if (_battleRoundsMachine != null)
        {
            _battleRoundsMachine.BattleFinished += HandleBattleFinished;
        }

        _sm.Configure(BattlePhase.Loading)
            .Permit(BattleTrigger.StartBattle, BattlePhase.Tactics);

        _sm.Configure(BattlePhase.Tactics)
            .OnEntry(() => OnEnterTactics())
            .OnExit(() => OnExitTactics())
            .Permit(BattleTrigger.StartBattleRound, BattlePhase.BattleRounds);

        _sm.Configure(BattlePhase.BattleRounds)
            .OnEntry(() => OnEnterRounds())
            .OnExit(() => OnExitRounds())
            .Permit(BattleTrigger.ShowBattleResults, BattlePhase.Results);

        _sm.Configure(BattlePhase.Results)
            .OnEntry(() => OnEnterResults())
            .OnExit(() => OnExitResults());
    }

    public BattlePhase State => _sm.State;

    public void Fire(BattleTrigger trigger)
    {
        if (_sm.CanFire(trigger)) _sm.Fire(trigger);
    }

    private void OnEnterTactics()
    {
        _ctx.PanelManager?.Show("tactic");
        _ctx.BattleTacticUIController.OnStartCombat += HandleStartCombat;
        _ctx.BattleGridDragAndDropController.enabled = true;

        PlaceUnitsOnGrid();
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
        _ctx.BattleResultsUIController?.Render(_ctx.BattleResult);
    }

    private void OnExitTactics()
    {
        _ctx.BattleGridController.DisableSlotsCollider();
        _ctx.BattleTacticUIController.OnStartCombat -= HandleStartCombat;
        _ctx.BattleGridDragAndDropController.enabled = false;
    }

    private void OnExitRounds()
    {
        // No actions needed on exit from rounds phase currently.
    }

    private void OnExitResults()
    {
        // No actions needed on exit from results phase currently.
    }

    private void HandleBattleFinished(BattleResult result)
    {
        _ctx.BattleResult = result;
        Fire(BattleTrigger.ShowBattleResults);
    }

    private void PlaceUnitsOnGrid()
    {
        if (_ctx.BattleGridController == null || _ctx.BattleUnits == null || _ctx.BattleUnits.Count == 0)
            return;
        if (!_ctx.BattleGridController.TryPlaceUnits(_ctx.BattleUnits))
        {
            Debug.LogWarning("Failed to place battle units on the grid.");
        }
    }

    private void HandleStartCombat()
    {
        Fire(BattleTrigger.StartBattleRound);
    }
}
