using Stateless;
using UnityEngine;

public sealed class BattlePhasesMachine
{
    private BattleResult _battleResult;
    private readonly BattleContext _ctx;
    private readonly BattleRoundsMachine _battleRoundsMachine;
    private readonly StateMachine<BattlePhaseStates, BattlePhasesTrigger> _sm;

    public BattleResult BattleResult => _battleResult;

    public BattlePhasesMachine(BattleContext ctx, BattleRoundsMachine battleRoundsMachine)
    {
        _ctx = ctx;
        _battleRoundsMachine = battleRoundsMachine;
        _sm = new StateMachine<BattlePhaseStates, BattlePhasesTrigger>(BattlePhaseStates.Loading);

        _battleRoundsMachine.OnBattleRoundsFinished += HandleBattleFinished;

        BattleLogger.LogPhaseEntered(BattlePhaseStates.Loading);

        _sm.Configure(BattlePhaseStates.Loading)
            .Permit(BattlePhasesTrigger.StartBattle, BattlePhaseStates.Tactics);

        _sm.Configure(BattlePhaseStates.Tactics)
            .OnEntry(() => OnEnterTactics())
            .OnExit(() => OnExitTactics())
            .Permit(BattlePhasesTrigger.StartBattleRound, BattlePhaseStates.BattleRounds);

        _sm.Configure(BattlePhaseStates.BattleRounds)
            .OnEntry(() => OnEnterRounds())
            .OnExit(() => OnExitRounds())
            .Permit(BattlePhasesTrigger.ShowBattleResults, BattlePhaseStates.Results);

        _sm.Configure(BattlePhaseStates.Results)
            .OnEntry(() => OnEnterResults())
            .OnExit(() => OnExitResults());
    }

    public void Fire(BattlePhasesTrigger trigger)
    {
        if (_sm.CanFire(trigger)) _sm.Fire(trigger);
    }

    private void OnEnterTactics()
    {
        BattleLogger.LogPhaseEntered(BattlePhaseStates.Tactics);
        SubscribeToGameEvents();
        _ctx.BattleSceneUIController.ShowPanel(BattleSceneUIController.PanelName.TacticPanel);
        if (!_ctx.BattleGridController.TryPlaceUnits(_ctx.BattleUnits))
        {
            Debug.LogWarning($"[{nameof(BattlePhasesMachine)}.{nameof(OnEnterTactics)}] Failed to place battle units on the grid.");
        }
        _ctx.BattleGridDragAndDropController.enabled = true;
    }

    private void OnEnterRounds()
    {
        BattleLogger.LogPhaseEntered(BattlePhaseStates.BattleRounds);
        _ctx.BattleSceneUIController.ShowPanel(BattleSceneUIController.PanelName.CombatPanel);
        _battleRoundsMachine.Reset();
        _battleRoundsMachine.BeginRounds();
    }

    private void OnEnterResults()
    {
        BattleLogger.LogPhaseEntered(BattlePhaseStates.Results);
        _ctx.IsFinished = true;
        _ctx.BattleSceneUIController.ShowPanel(BattleSceneUIController.PanelName.ResultPanel);
        _ctx.BattleSceneUIController.ShowResult(_battleResult);

        if (_ctx.BattleUnits != null)
        {
            foreach (var unit in _ctx.BattleUnits)
            {
                unit.SetInteractionEnabled(false);
            }
        }
    }

    private void OnExitTactics()
    {
        _ctx.BattleGridController.DisableSlotsCollider();
        _ctx.BattleGridDragAndDropController.enabled = false;
    }

    private void OnExitRounds()
    {
        // No actions needed on exit from rounds phase currently.
    }

    private void OnExitResults()
    {
        // No actions needed on exit from results phase currently.
        UnsubscribeFromGameEvents();
    }

    private void SubscribeToGameEvents()
    {
        _ctx.SceneEventBusService.Subscribe<RequestStartCombat>(HandleStartCombat);
    }

    private void UnsubscribeFromGameEvents()
    {
        _ctx.SceneEventBusService.Unsubscribe<RequestStartCombat>(HandleStartCombat);
    }

    private void HandleBattleFinished(BattleResult result)
    {
        _battleResult = result;
        Fire(BattlePhasesTrigger.ShowBattleResults);
    }

    private void HandleStartCombat(RequestStartCombat evt)
    {
        Fire(BattlePhasesTrigger.StartBattleRound);
    }
}
