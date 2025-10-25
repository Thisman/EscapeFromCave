using System;

public class PlayerBattleActionController : IBattleActionController
{
    private IBattleContext _ctx;
    private Action<IBattleAction> _onActionReady;

    public void RequestAction(IBattleContext ctx, Action<IBattleAction> onActionReady)
    {
        _ctx = ctx;
        _onActionReady = onActionReady;
        _ctx.BattleCombatUIController.OnDefend += HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn += HandleSkipTurn;

        onActionReady.Invoke(new AttackAction(ctx));
    }

    private void HandleDefend()     {
        _ctx.BattleCombatUIController.OnDefend -= HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn -= HandleSkipTurn;
        var defendAction = new DefendAction();
        _onActionReady.Invoke(defendAction);
    }

    private void HandleSkipTurn()
    {
        _ctx.BattleCombatUIController.OnDefend -= HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn -= HandleSkipTurn;
        var skipTurnAction = new SkipTurnAction();
        _onActionReady.Invoke(skipTurnAction);
    }
}
