using System;

public sealed class BattleEffectModel : IReadonlyBattleEffectModel
{
    private readonly BattleEffectDefinitionSO _definition;

    public BattleEffectModel(BattleEffectDefinitionSO definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public BattleEffectDefinitionSO Definition => _definition;

    public void Attach(BattleContext ctx)
    {
        _definition.OnAttach(ctx);
    }

    public void Apply(BattleContext ctx)
    {
        _definition.OnApply(ctx);
    }

    public void Tick(BattleContext ctx)
    {
        _definition.OnTick(ctx);
    }

    public void OnBattleRoundState(BattleContext ctx)
    {
        _definition.OnBattleRoundState(ctx);
    }

    public void Remove(BattleContext ctx)
    {
        _definition.OnRemove(ctx);
    }
}
