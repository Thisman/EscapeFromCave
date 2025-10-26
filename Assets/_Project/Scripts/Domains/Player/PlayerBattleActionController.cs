using System;

public class PlayerBattleActionController : IBattleActionController
{
    private IBattleContext _ctx;
    private Action<IBattleAction> _onActionReady;

    public void RequestAction(IBattleContext ctx, Action<IBattleAction> onActionReady)
    {
        _ctx = ctx;
        _onActionReady = onActionReady;

        // Unsubscribe prev handlers
        _ctx.BattleCombatUIController.OnDefend -= HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn -= HandleSkipTurn;

        _ctx.BattleCombatUIController.OnDefend += HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn += HandleSkipTurn;

        var targetResolver = new DefaultActionTargetResolver(ctx);
        var damageResolver = new DefaultBattleDamageResolver();
        var targetPicker = new PlayerActionTargetPicker(ctx, targetResolver);
        onActionReady.Invoke(new AttackAction(ctx, targetResolver, damageResolver, targetPicker));
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
