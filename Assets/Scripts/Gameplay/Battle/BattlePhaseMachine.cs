using Stateless;
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

        _battleRoundsMachine.OnBattleRoundsFinished += HandleBattleFinished;

        BattleLogger.LogPhaseEntered(BattlePhase.Loading);

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

    public void Fire(BattleTrigger trigger)
    {
        if (_sm.CanFire(trigger)) _sm.Fire(trigger);
    }

    private void OnEnterTactics()
    {
        BattleLogger.LogPhaseEntered(BattlePhase.Tactics);
        _ctx.BattleUIController?.ShowPanel(BattleUIController.PanelName.TacticPanel);
        if (_ctx.BattleUIController != null)
            _ctx.BattleUIController.OnStartCombat += HandleStartBattleRounds;
        if (!_ctx.BattleGridController.TryPlaceUnits(_ctx.BattleUnits))
        {
            Debug.LogWarning($"[{nameof(BattlePhaseMachine)}.{nameof(OnEnterTactics)}] Failed to place battle units on the grid.");
        }
        _ctx.BattleGridDragAndDropController.enabled = true;
    }

    private void OnEnterRounds()
    {
        BattleLogger.LogPhaseEntered(BattlePhase.BattleRounds);
        _ctx.BattleUIController?.ShowPanel(BattleUIController.PanelName.CombatPanel);
        _battleRoundsMachine.Reset();
        _battleRoundsMachine.BeginRounds();
    }

    private void OnEnterResults()
    {
        BattleLogger.LogPhaseEntered(BattlePhase.Results);
        _ctx.IsFinished = true;
        _ctx.BattleUIController?.ShowPanel(BattleUIController.PanelName.ResultPanel);
        _ctx.BattleUIController?.ShowResult(_ctx.BattleResult);
    }

    private void OnExitTactics()
    {
        _ctx.BattleGridController.DisableSlotsCollider();
        if (_ctx.BattleUIController != null)
            _ctx.BattleUIController.OnStartCombat -= HandleStartBattleRounds;
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

    private void HandleStartBattleRounds()
    {
        Fire(BattleTrigger.StartBattleRound);
    }
}
