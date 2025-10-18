using System.Collections.Generic;

public interface IGameSession
{
    IUnitDefinition Hero { get; }
    IReadOnlyList<IUnitDefinition> Army { get; }
    bool HasSelection { get; }
    void SetSelection(IUnitDefinition hero, List<IUnitDefinition> army);
    void Clear();
}