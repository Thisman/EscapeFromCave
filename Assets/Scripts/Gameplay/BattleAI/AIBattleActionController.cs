using System;
using System.Threading.Tasks;

public class AIBattleActionController : IBattleActionController
{
    private const int MinActionDelayMilliseconds = 500;
    private const int MaxActionDelayMilliseconds = 1250;
    private static readonly Random DelayRandomizer = new();

    private BattleContext _ctx;
    private IBattleAction _currentAction;
    private int _currentActionId;

    public async void RequestAction(BattleContext ctx, int actionId)
    {
        _ctx = ctx;
        _currentActionId = actionId;

        int delay = GetRandomActionDelay();
        await Task.Delay(delay);

        var targetResolver = new BattleActionTargetResolverForAttack(ctx);
        var damageResolver = new BattleDamageResolverByDefault();
        var targetPicker = new BattlActionTargetPickerForAI(ctx);
        SelectAction(new BattleActionAttack(ctx, targetResolver, damageResolver, targetPicker));
    }

    private void SelectAction(IBattleAction action)
    {
        if (action == null || _ctx == null)
            return;

        ClearCurrentAction();
        _currentAction = action;
        SubscribeToActionLifecycle(_currentAction);
        _ctx.SceneEventBusService?.Publish(new BattleActionSelected(_currentAction, _ctx.ActiveUnit, _currentActionId));
        _currentAction.Resolve();
    }

    private void SubscribeToActionLifecycle(IBattleAction action)
    {
        action.OnResolve += HandleActionResolved;
        action.OnCancel += HandleActionCancelled;
    }

    private void UnsubscribeFromActionLifecycle(IBattleAction action)
    {
        action.OnResolve -= HandleActionResolved;
        action.OnCancel -= HandleActionCancelled;
    }

    private void ClearCurrentAction()
    {
        if (_currentAction == null)
            return;

        UnsubscribeFromActionLifecycle(_currentAction);

        if (_currentAction is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _currentAction = null;
    }

    private void HandleActionResolved()
    {
        if (_ctx == null)
            return;

        if (_currentAction != null)
        {
            UnsubscribeFromActionLifecycle(_currentAction);
        }

        _ctx.SceneEventBusService?.Publish(new BattleActionResolved(_currentAction, _ctx.ActiveUnit, _currentActionId));
        _currentAction = null;
    }

    private void HandleActionCancelled()
    {
        if (_ctx == null)
            return;

        if (_currentAction != null)
        {
            UnsubscribeFromActionLifecycle(_currentAction);
        }

        _ctx.SceneEventBusService?.Publish(new BattleActionCancelled(_ctx.ActiveUnit, _currentActionId));
        _currentAction = null;
    }

    private static int GetRandomActionDelay()
    {
        int min = Math.Max(0, MinActionDelayMilliseconds);
        int max = Math.Max(min, MaxActionDelayMilliseconds);

        lock (DelayRandomizer)
        {
            // Random.Next upper bound is exclusive, so add 1 to make it inclusive.
            return DelayRandomizer.Next(min, max + 1);
        }
    }
}
