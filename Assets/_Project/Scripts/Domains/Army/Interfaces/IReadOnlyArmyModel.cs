using System;
using System.Collections.Generic;

public interface IReadOnlyArmyModel
{
    int MaxSlots { get; }
    IReadOnlyList<IReadOnlySquadModel> Slots { get; }
    event Action<IReadOnlyArmyModel> Changed;

    IReadOnlySquadModel GetSlot(int index);
    IReadOnlyList<IReadOnlySquadModel> GetAllSlots();
}
