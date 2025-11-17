using System.Collections.Generic;
using UnityEngine;

public readonly struct SquadSelection
{
    public SquadSelection(UnitSO definition, int count)
    {
        Definition = definition;
        Count = Mathf.Max(1, count);
    }

    public UnitSO Definition { get; }

    public int Count { get; }
}

public sealed class GameSession
{
    private UnitSO _selectedHero;

    private readonly List<SquadSelection> _selectedAllySquads = new();

    public UnitSO SelectedHero => _selectedHero;

    public IReadOnlyList<SquadSelection> SelectedAllySquads => _selectedAllySquads;

    public void SaveSelectedHeroSquads(UnitSO heroDefinition, List<SquadSelection> armyDefinition)
    {
        _selectedHero = heroDefinition;
        _selectedAllySquads.Clear();
        if (armyDefinition != null)
            _selectedAllySquads.AddRange(armyDefinition);

        string heroName = _selectedHero != null ? _selectedHero.name : "<none>";
        Debug.Log($"[{nameof(GameSession)}.{nameof(SaveSelectedHeroSquads)}] Selection updated. Hero: {heroName}, Army size: {_selectedAllySquads.Count}.");
    }

    public void Clear()
    {
        _selectedHero = null;
        _selectedAllySquads.Clear();
        Debug.Log($"[{nameof(GameSession)}.{nameof(Clear)}] Selection cleared.");
    }
}
