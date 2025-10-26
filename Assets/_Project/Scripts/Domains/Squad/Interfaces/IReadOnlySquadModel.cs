using System;

public interface IReadOnlySquadModel
{
    UnitDefinitionSO Definition { get; }

    int Count { get; }

    bool IsEmpty { get; }

    event Action<IReadOnlySquadModel> Changed;
}
