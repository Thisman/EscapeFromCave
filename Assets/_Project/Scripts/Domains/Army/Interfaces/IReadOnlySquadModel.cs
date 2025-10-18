using System;

public interface IReadOnlySquadModel
{
    UnitDefinitionSO UnitDefinition { get; }
    int Count { get; }
    bool IsEmpty { get; }
    event Action<IReadOnlySquadModel> Changed;
}
