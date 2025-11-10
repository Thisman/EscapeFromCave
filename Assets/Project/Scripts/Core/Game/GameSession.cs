using System.Collections.Generic;
using UnityEngine;

public sealed class GameSession
{
    private UnitSO _selectedHero;

    private readonly List<UnitSO> _selectedAllySquads = new();

    public UnitSO SelectedHero => _selectedHero;

    public IReadOnlyList<UnitSO> SelectedAllySquads => _selectedAllySquads;

    public void SaveSelectedHeroSquads(UnitSO heroDefinition, List<UnitSO> armyDefinition)
    {
        _selectedHero = heroDefinition;
        _selectedAllySquads.Clear();
        _selectedAllySquads.AddRange(armyDefinition);

        GameLogger.Log($"Selection updated. Hero: {_selectedHero.name}, Army size: {_selectedAllySquads.Count}.");
    }

    public void Clear()
    {
        _selectedHero = null;
        _selectedAllySquads.Clear();
        GameLogger.Log("Selection cleared.");
    }
}
