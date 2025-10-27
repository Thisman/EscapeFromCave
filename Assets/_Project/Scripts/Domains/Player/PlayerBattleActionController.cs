using System;

public class PlayerBattleActionController : IBattleActionController
{
    private BattleContext _ctx;
    private Action<IBattleAction> _onActionReady;

    public void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady)
    {
        _ctx = ctx;
        _onActionReady = onActionReady;

        // Unsubscribe prev handlers
        UnsubscribeUIEvents();
        SubscribeUIEvents();
        UpdateDefendAvailability();

        var targetResolver = new DefaultActionTargetResolver(ctx);
        var damageResolver = new DefaultBattleDamageResolver();
        var targetPicker = new PlayerBattleActionTargetPicker(ctx, targetResolver);

        onActionReady.Invoke(new AttackAction(ctx, targetResolver, damageResolver, targetPicker));
    }

    private void HandleDefend()
    {
        if (!CanActiveUnitDefend())
        {
            _ctx.BattleCombatUIController.SetDefendButtonInteractable(false);
            return;
        }

        UnsubscribeUIEvents();
        _ctx.BattleCombatUIController.SetDefendButtonInteractable(false);
        var defendAction = new DefendAction();
        _onActionReady.Invoke(defendAction);
    }

    private void HandleSkipTurn()
    {
        UnsubscribeUIEvents();
        _ctx.BattleCombatUIController.SetDefendButtonInteractable(false);
        var skipTurnAction = new SkipTurnAction();
        _onActionReady.Invoke(skipTurnAction);
    }

    private bool CanActiveUnitDefend()
    {
        var activeUnit = _ctx.ActiveUnit;
        var defendedUnits = _ctx.DefendedUnitsThisRound;

        return defendedUnits == null || !defendedUnits.Contains(activeUnit);
    }

    private void UpdateDefendAvailability()
    {
        bool canDefend = CanActiveUnitDefend();
        _ctx.BattleCombatUIController.SetDefendButtonInteractable(canDefend);
    }

    private void SubscribeUIEvents()
    {
        _ctx.BattleCombatUIController.OnDefend += HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn += HandleSkipTurn;
    }

    private void UnsubscribeUIEvents()
    {
        _ctx.BattleCombatUIController.OnDefend -= HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn -= HandleSkipTurn;
    }
}
