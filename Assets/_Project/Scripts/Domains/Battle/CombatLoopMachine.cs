using Stateless;

public sealed class CombatLoopMachine
{
    private readonly IBattleContext _ctx;
    private readonly StateMachine<CombatState, CombatTrigger> _sm;
    private readonly ActionPipelineMachine _action;

    public CombatLoopMachine(IBattleContext ctx, ActionPipelineMachine action)
    {
        _ctx = ctx;
        _action = action;
        _sm = new StateMachine<CombatState, CombatTrigger>(CombatState.RoundInit);

        _sm.Configure(CombatState.RoundInit)
            .OnEntry(RoundInit)
            .Permit(CombatTrigger.BeginRound, CombatState.TurnSelect);

        _sm.Configure(CombatState.TurnSelect)
            .OnEntry(TurnSelect)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnStart)
            .Permit(CombatTrigger.Skip, CombatState.TurnSkip)
            .Permit(CombatTrigger.QueueEmpty, CombatState.RoundEnd);

        _sm.Configure(CombatState.TurnStart)
            .OnEntry(TurnStart)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnActionHost);

        _sm.Configure(CombatState.TurnActionHost)
            .OnEntry(TurnActionHost)
            .Permit(CombatTrigger.ActionDone, CombatState.TurnEnd);

        _sm.Configure(CombatState.TurnSkip)
            .OnEntry(TurnSkip)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnEnd);

        _sm.Configure(CombatState.TurnEnd)
            .OnEntry(TurnEnd)
            .Permit(CombatTrigger.NextTurn, CombatState.TurnSelect)
            .Permit(CombatTrigger.QueueEmpty, CombatState.RoundEnd);

        _sm.Configure(CombatState.RoundEnd)
            .OnEntry(RoundEnd)
            .Permit(CombatTrigger.EndRound, CombatState.RoundInit)
            .Permit(CombatTrigger.EndCombat, CombatState.RoundEnd); // будет обработано фазовой машиной
    }

    public CombatState State => _sm.State;

    public void Reset() => _sm.Activate(); // опционально
    public void BeginRound() => _sm.Fire(CombatTrigger.BeginRound);

    // ---- Handlers ----
    private void RoundInit()
    {
        // пересбор инициативы, onRoundStart
    }

    private void TurnSelect()
    {
        // если очередь пуста:
        //   _sm.Fire(CombatTrigger.QueueEmpty);
        // else если активный не может ходить:
        //   _sm.Fire(CombatTrigger.Skip);
        // else:
        //   _sm.Fire(CombatTrigger.NextTurn);
    }

    private void TurnStart()
    {
        // onTurnStart(), тики статусов
        _sm.Fire(CombatTrigger.NextTurn); // к действию
    }

    private void TurnActionHost()
    {
        _action.ResetForCurrentActor(); // скормить активного юнита внутрь ActionFSM
        _action.RunToEnd();             // синхронно или await-версия
        _sm.Fire(CombatTrigger.ActionDone);
    }

    private void TurnSkip()
    {
        // логируем пропуск, публикуем события
        _sm.Fire(CombatTrigger.NextTurn);
    }

    private void TurnEnd()
    {
        // onTurnEnd(), истечения, переинициализация очереди
        // if очередь пуста → QueueEmpty; else → NextTurn
    }

    private void RoundEnd()
    {
        // onRoundEnd(), проверка конца боя:
        // если бой завершён → фазовая машина должна вызвать EndCombat
        // иначе новый раунд:
        _sm.Fire(CombatTrigger.EndRound);
    }
}
