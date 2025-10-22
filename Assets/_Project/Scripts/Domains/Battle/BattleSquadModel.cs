using System;

public sealed class BattleSquadModel : IReadOnlyBattleSquadModel
{
    public SquadModel Squad { get; }

    IReadOnlySquadModel IReadOnlyBattleSquadModel.Squad => Squad;

    public BattleSquadModel(UnitModel unit)
    {
        if (unit == null) throw new ArgumentNullException(nameof(unit));
        if (unit.Definition == null) throw new ArgumentException("Unit definition is required.", nameof(unit));

        Squad = new SquadModel(unit.Definition, 1);
    }

    public BattleSquadModel(SquadModel squad)
    {
        Squad = squad ?? throw new ArgumentNullException(nameof(squad));
    }
}
