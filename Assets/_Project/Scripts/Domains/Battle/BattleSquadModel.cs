using System;

public sealed class BattleSquadModel : IReadOnlyBattleSquadModel
{
    private readonly SquadModel _squadModel;

    public BattleSquadModel(SquadModel squadModel)
    {
        _squadModel = squadModel ?? throw new ArgumentNullException(nameof(squadModel));
    }

    public UnitDefinitionSO UnitDefinition => _squadModel.UnitDefinition;

    public int Count => _squadModel.Count;

    public bool IsEmpty => _squadModel.IsEmpty;

    public event Action<IReadOnlySquadModel> Changed
    {
        add => _squadModel.Changed += value;
        remove => _squadModel.Changed -= value;
    }

    public SquadModel GetBaseModel()
    {
        return _squadModel;
    }
}
