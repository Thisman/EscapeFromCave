using System.Collections.Generic;

public sealed class GameSession : IGameSession
{
    private IUnitDefinition _hero;
    private readonly List<IUnitDefinition> _army = new();
    public IUnitDefinition Hero => _hero;
    public IReadOnlyList<IUnitDefinition> Army => _army;
    public bool HasSelection => _hero != null && _army.Count > 0;

    public void SetSelection(IUnitDefinition hero, List<IUnitDefinition> army)
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
