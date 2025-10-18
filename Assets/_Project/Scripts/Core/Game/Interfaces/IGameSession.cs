using System.Collections.Generic;

public interface IGameSession
{
    UnitDefinitionSO Hero { get; }

    IReadOnlyList<UnitDefinitionSO> Army { get; }

    bool HasSelection { get; }

    void SetSelection(UnitDefinitionSO hero, List<UnitDefinitionSO> army);

    void Clear();
}