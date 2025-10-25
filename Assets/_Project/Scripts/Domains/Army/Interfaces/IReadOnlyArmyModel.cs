using System;
using System.Collections.Generic;

public interface IReadOnlyArmyModel
{
    int MaxSlots { get; }

    event Action<IReadOnlyArmyModel> Changed;

    IReadOnlyList<IReadOnlySquadModel> Slots { get; }

    IReadOnlySquadModel GetSlot(int index);

    IReadOnlyList<IReadOnlySquadModel> GetAllSlots();
}
