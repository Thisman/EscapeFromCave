using System;
using UnityEngine.InputSystem;

public class PlayerBattleActionController : IBattleActionController
{
    private BattleContext _ctx;
    private Action<IBattleAction> _onActionReady;
    private InputAction _cancelAction;

    public void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady)
    {
        _ctx = ctx;
        _onActionReady = onActionReady;

        // Unsubscribe prev handlers
        UnsubscribeUIEvents();
        SubscribeUIEvents();
        UpdateDefendAvailability();

        var targetResolver = new BattleActionDefaultTargetResolver(ctx);
        var damageResolver = new BattleDamageDefaultResolver();
        var targetPicker = new PlayerBattleActionTargetPicker(ctx, targetResolver);

        onActionReady.Invoke(new BattleActionAttack(ctx, targetResolver, damageResolver, targetPicker));
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
        var defendAction = new BattleActionDefend();
        _onActionReady.Invoke(defendAction);
    }

    private void HandleSkipTurn()
    {
        UnsubscribeUIEvents();
        _ctx.BattleCombatUIController.SetDefendButtonInteractable(false);
        var skipTurnAction = new BattleActionSkipTurn();
        _onActionReady.Invoke(skipTurnAction);
    }

    private void HandleAbilitySelected(BattleAbilitySO ability)
    {
        if (ability == null || _ctx == null || _onActionReady == null)
            return;

        var abilityManager = _ctx.BattleAbilitiesManager;
        var activeUnit = _ctx.ActiveUnit;
        if (abilityManager != null && activeUnit != null && !abilityManager.IsAbilityReady(activeUnit, ability))
        {
            return;
        }

        IBattleActionTargetResolver targetResolver = ability.AbilityTargetType switch
        {
            BattleAbilityTargetType.Self => new BattleActionSelfTargetResolver(),
            BattleAbilityTargetType.Ally => new BattleActionAllyTargetResolver(),
            BattleAbilityTargetType.AllAllies => new BattleActionAllyTargetResolver(),
            BattleAbilityTargetType.SingleEnemy => new BattleActionEnemyTargetResolver(),
            BattleAbilityTargetType.AllEnemies => new BattleActionEnemyTargetResolver(),
            _ => new BattleActionEnemyTargetResolver(),
        };

        var targetPicker = new PlayerBattleActionTargetPicker(_ctx, targetResolver);
        var abilityAction = new BattleActionAbility(_ctx, ability, targetResolver, targetPicker);
        _onActionReady.Invoke(abilityAction);
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
        _ctx.BattleCombatUIController.OnSelectAbility += HandleAbilitySelected;

        SubscribeToCancelAction();
    }

    private void UnsubscribeUIEvents()
    {
        _ctx.BattleCombatUIController.OnDefend -= HandleDefend;
        _ctx.BattleCombatUIController.OnSkipTurn -= HandleSkipTurn;
        _ctx.BattleCombatUIController.OnSelectAbility -= HandleAbilitySelected;

        UnsubscribeFromCancelAction();
    }

    private void SubscribeToCancelAction()
    {
        if (_cancelAction != null)
            return;

        var inputService = _ctx?.InputService;
        if (inputService == null)
            return;

        _cancelAction = inputService.Actions.FindAction("Cancel");
        if (_cancelAction == null)
            return;

        _cancelAction.performed += HandleCancelPerformed;
    }

    private void UnsubscribeFromCancelAction()
    {
        if (_cancelAction == null)
            return;

        _cancelAction.performed -= HandleCancelPerformed;
        _cancelAction = null;
    }

    private void HandleCancelPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (_ctx?.CurrentAction is BattleActionAbility abilityAction)
        {
            abilityAction.Dispose();
        }
    }
}
