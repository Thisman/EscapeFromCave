using Stateless;

public sealed class ActionPipelineMachine
{
    private readonly IBattleContext _ctx;
    private readonly StateMachine<ActionState, ActionTrigger> _sm;

    public ActionPipelineMachine(IBattleContext ctx)
    {
        _ctx = ctx;
        _sm = new StateMachine<ActionState, ActionTrigger>(ActionState.AbilitySelect);

        _sm.Configure(ActionState.AbilitySelect)
            .OnEntry(AbilitySelect)
            .Permit(ActionTrigger.Commit, ActionState.CommitBuiltin)
            .Permit(ActionTrigger.ChooseAbility, ActionState.TargetSelect)
            .Permit(ActionTrigger.Cancel, ActionState.Cancel);

        _sm.Configure(ActionState.TargetSelect)
            .OnEntry(TargetSelect)
            .Permit(ActionTrigger.ChooseTargets, ActionState.Validate)
            .Permit(ActionTrigger.Cancel, ActionState.AbilitySelect);

        _sm.Configure(ActionState.Validate)
            .OnEntry(Validate)
            .Permit(ActionTrigger.Valid, ActionState.Resolve)
            .Permit(ActionTrigger.Invalid, ActionState.TargetSelect);

        _sm.Configure(ActionState.Resolve)
            .OnEntry(Resolve)
            .Permit(ActionTrigger.Finish, ActionState.End);

        _sm.Configure(ActionState.CommitBuiltin)
            .OnEntry(CommitBuiltin)
            .Permit(ActionTrigger.Finish, ActionState.End);

        _sm.Configure(ActionState.Cancel)
            .OnEntry(Cancel)
            .Permit(ActionTrigger.Finish, ActionState.End);

        _sm.Configure(ActionState.End)
            .OnEntry(End);
    }

    public ActionState State => _sm.State;

    public void ResetForCurrentActor()
    {
        // передать текущего актора/список способностей/контекст в локальные поля
        _sm.Activate(); // или вручную сбросить на AbilitySelect
    }

    public void RunToEnd()
    {
        // Для MVP — синхронный прогон; позже можно сделать async/await
        while (State != ActionState.End)
        {
            // при OnEntry хендлеры сами вызывают Fire нужных триггеров
        }
    }

    private void AbilitySelect()
    {
        // контроллер (игрок/AI) выбирает "Defend/Wait/Skip/Surrender" → Fire(Commit)
        // либо конкретную ability → Fire(ChooseAbility)
    }

    private void TargetSelect()
    {
        // выбрать цели → Fire(ChooseTargets)
        // отмена → Fire(Cancel)
    }

    private void Validate()
    {
        // валидатор → Ok? Fire(Valid) : Fire(Invalid)
    }

    private void Resolve()
    {
        // Pre → Cost → ApplyEffects → Post → Fire(Finish)
    }

    private void CommitBuiltin()
    {
        // Defend/Wait/Skip/Surrender → Fire(Finish)
    }

    private void Cancel()
    {
        // лог причины → Fire(Finish)
    }

    private void End()
    {
        // no-op
    }
}
