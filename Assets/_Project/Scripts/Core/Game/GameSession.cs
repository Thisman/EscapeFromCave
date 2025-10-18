using System.Collections.Generic;

public sealed class GameSession : IGameSession
{
    private UnitDefinitionSO _hero;

    private readonly List<UnitDefinitionSO> _army = new();

    public UnitDefinitionSO Hero => _hero;

    public IReadOnlyList<UnitDefinitionSO> Army => _army;

    public bool HasSelection => _hero != null && _army.Count > 0;

    public void SetSelection(UnitDefinitionSO hero, List<UnitDefinitionSO> army)
    {
        _hero = hero;
        _army.Clear();
        if (army != null) _army.AddRange(army);
    }

    public void Clear()
    {
        _hero = null;
        _army.Clear();
    }
}
