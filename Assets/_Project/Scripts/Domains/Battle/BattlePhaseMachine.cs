using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BattlePhaseMachine
{
    private readonly StateMachine<BattlePhase, BattleTrigger> _sm;
    private readonly IBattleContext _ctx;
    private readonly CombatLoopMachine _combat;

    public BattlePhaseMachine(IBattleContext ctx, CombatLoopMachine combat)
    {
        _ctx = ctx;
        _combat = combat;
        _sm = new StateMachine<BattlePhase, BattleTrigger>(BattlePhase.Loading);

        _sm.Configure(BattlePhase.Loading)
            .Permit(BattleTrigger.Start, BattlePhase.Tactics);

        _sm.Configure(BattlePhase.Tactics)
            .OnEntry(() => OnEnterTactics())
            .OnExit(() => OnExitTactics())
            .Permit(BattleTrigger.EndTactics, BattlePhase.Combat);

        _sm.Configure(BattlePhase.Combat)
            .OnEntry(() => OnEnterCombat())
            .Permit(BattleTrigger.EndCombat, BattlePhase.Results);

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
        _ctx.PanelController?.Show("tactic");
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

    private void OnEnterCombat()
    {
        _ctx.PanelController?.Show("combat");
        IEnumerable<IReadOnlyUnitModel> unitModels = _ctx.BattleUnits.Select(u => u.GetComponent<BattleUnitController>().GetUnitModel());
        _ctx.BattleQueueController.Rebuild(unitModels);
        _ctx.BattleQueueUIController?.Init(_ctx.BattleQueueController);
        _combat.Reset();
        _combat.BeginRound(); // инициируем первый раунд
    }

    private void OnEnterResults()
    {
        _ctx.PanelController?.Show("results");
        _ctx.IsFinished = true;
        // сериализация результатов, подсчёт лута/опыта
    }

    private void OnExitTactics()
    {
        _ctx.BattleGridDragAndDropController.enabled = false;
    }
}
