using System;
using UnityEngine.InputSystem;

public class PlayerBattleActionController : IBattleActionController
{
    private BattleContext _ctx;
    private InputAction _cancelAction;

    private Action<IBattleAction> _onActionReady;

    public void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady)
    {
        _ctx = ctx;
        _onActionReady = onActionReady;

        // Unsubscribe prev handlers
        UnsubscribeGameEvents();
        UnsubscribeUIEvents();
        SubscribeGameEvents();
        SubscribeUIEvents();
        UpdateDefendAvailability();

        var targetResolver = new BattleActionTargetResolverForAttack(ctx);
        var damageResolver = new BattleDamageResolverByDefault();
        var targetPicker = new BattleActionTargetPickerForPlayer(ctx, targetResolver);

        onActionReady.Invoke(new BattleActionAttack(ctx, targetResolver, damageResolver, targetPicker));
    }

    private void HandleDefend()
    {
        if (!CanActiveUnitDefend())
        {
            _ctx.BattleSceneUIController?.SetDefendButtonInteractable(false);
            return;
        }

        UnsubscribeGameEvents();
        UnsubscribeUIEvents();
        _ctx.BattleSceneUIController?.SetDefendButtonInteractable(false);
        var defendAction = new BattleActionDefend();
        _onActionReady.Invoke(defendAction);
    }

    private void HandleSkipTurn()
    {
        UnsubscribeGameEvents();
        UnsubscribeUIEvents();
        _ctx.BattleSceneUIController?.SetDefendButtonInteractable(false);
        var skipTurnAction = new BattleActionSkipTurn();
        _onActionReady.Invoke(skipTurnAction);
    }

    private void HandleDefendRequest(RequestDefend evt)
    {
        HandleDefend();
    }

    private void HandleSkipTurnRequest(RequestSkipTurn evt)
    {
        HandleSkipTurn();
    }

    private void HandleAbilitySelected(RequestSelectAbility request)
    {
        var ability = request?.Ability;
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
            BattleAbilityTargetType.Self => new BattleActionTargetResolverForSelf(),
            BattleAbilityTargetType.SingleAlly => new BattleActionTargetResolverForAlly(),
            BattleAbilityTargetType.AllAllies => new BattleActionTargetResolverForAlly(),
            BattleAbilityTargetType.SingleEnemy => new BattleActionTargetResolverForEnemy(),
            BattleAbilityTargetType.AllEnemies => new BattleActionTargetResolverForEnemy(),
            _ => new BattleActionTargetResolverForEnemy(),
        };

        var targetPicker = new BattleActionTargetPickerForPlayer(_ctx, targetResolver);
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
        _ctx.BattleSceneUIController?.SetDefendButtonInteractable(canDefend);
    }

    private void SubscribeUIEvents()
    {
        SubscribeToCancelAction();
    }

    private void UnsubscribeUIEvents()
    {
        UnsubscribeFromCancelAction();
    }

    private void SubscribeGameEvents()
    {
        var sceneEventBus = _ctx?.SceneEventBusService;
        if (sceneEventBus == null)
            return;

        sceneEventBus.Subscribe<RequestDefend>(HandleDefendRequest);
        sceneEventBus.Subscribe<RequestSkipTurn>(HandleSkipTurnRequest);
        sceneEventBus.Subscribe<RequestSelectAbility>(HandleAbilitySelected);
    }

    private void UnsubscribeGameEvents()
    {
        var sceneEventBus = _ctx?.SceneEventBusService;
        if (sceneEventBus == null)
            return;

        sceneEventBus.Unsubscribe<RequestDefend>(HandleDefendRequest);
        sceneEventBus.Unsubscribe<RequestSkipTurn>(HandleSkipTurnRequest);
        sceneEventBus.Unsubscribe<RequestSelectAbility>(HandleAbilitySelected);
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
