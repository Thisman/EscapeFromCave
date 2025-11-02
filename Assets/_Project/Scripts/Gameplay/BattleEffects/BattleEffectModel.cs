using System;

public sealed class BattleEffectModel : IReadonlyBattleEffectModel
{
    public BattleEffectDefinitionSO Definition { get; }

    public IReadOnlySquadModel Target { get; }

    public int Stacks { get; private set; }

    public int RemainingDuration { get; private set; }

    public BattleEffectModel(BattleEffectDefinitionSO definition, IReadOnlySquadModel target)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Target = target ?? throw new ArgumentNullException(nameof(target));

        Stacks = 1;
        RemainingDuration = CalculateInitialDuration();
    }

    public void RefreshDuration()
    {
        if (Definition.DurationMode is BattleEffectDurationMode.TurnCount or BattleEffectDurationMode.RoundCount)
            RemainingDuration = Math.Max(0, Definition.Duration);
    }

    public void IncreaseStack()
    {
        if (Definition.MaxStacks <= 0)
        {
            Stacks++;
            return;
        }

        if (Stacks < Definition.MaxStacks)
            Stacks++;
    }

    public bool ShouldProcessTrigger(BattleRoundTrigger trigger)
    {
        return Definition.DurationMode switch
        {
            BattleEffectDurationMode.TurnCount => trigger is BattleRoundTrigger.NextTurn or BattleRoundTrigger.SkipTurn or BattleRoundTrigger.ActionDone,
            BattleEffectDurationMode.RoundCount => trigger is BattleRoundTrigger.StartNewRound or BattleRoundTrigger.EndRound,
            _ => false,
        };
    }

    public bool TickDuration()
    {
        if (Definition.DurationMode is not (BattleEffectDurationMode.TurnCount or BattleEffectDurationMode.RoundCount))
            return false;

        if (RemainingDuration > 0)
            RemainingDuration--;

        return RemainingDuration <= 0;
    }

    private int CalculateInitialDuration()
    {
        return Definition.DurationMode switch
        {
            BattleEffectDurationMode.TurnCount or BattleEffectDurationMode.RoundCount => Math.Max(0, Definition.Duration),
            _ => 0,
        };
    }
}
