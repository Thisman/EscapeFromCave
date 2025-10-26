using System;
using System.Collections.Generic;

public interface IReadOnlyArmyModel
{
    event Action<IReadOnlyArmyModel> Changed;

    IReadOnlyList<IReadOnlySquadModel> GetSquads();

    bool SetSlot(int index, SquadModel squad);

    bool TryAddSquad(UnitDefinitionSO def, int amount);

    bool ClearSlot(int index);
}
