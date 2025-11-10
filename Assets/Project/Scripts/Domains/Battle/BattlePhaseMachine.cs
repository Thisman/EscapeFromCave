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
        _ctx.BattleSquadInfoManager?.Enable();
        _ctx.PanelManager?.Show("tactic");
        _ctx.BattleTacticUIController.OnBattleRoundsStart += HandleStartBattleRounds;
        if (!_ctx.BattleGridController.TryPlaceUnits(_ctx.BattleUnits))
        {
            Debug.LogWarning($"[{nameof(BattlePhaseMachine)}.{nameof(OnEnterTactics)}] Failed to place battle units on the grid.");
        }
        _ctx.BattleGridDragAndDropController.enabled = true;
    }

    private void OnEnterRounds()
    {
        _ctx.PanelManager?.Show("rounds");
        _battleRoundsMachine.Reset();
        _battleRoundsMachine.BeginRound();
    }

    private void OnEnterResults()
    {
        _ctx.BattleSquadInfoManager?.Disable();
        _ctx.IsFinished = true;
        _ctx.PanelManager?.Show("results");
        _ctx.BattleResultsUIController?.Render(_ctx.BattleResult);
    }

    private void OnExitTactics()
    {
        _ctx.BattleGridController.DisableSlotsCollider();
        _ctx.BattleTacticUIController.OnBattleRoundsStart -= HandleStartBattleRounds;
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
