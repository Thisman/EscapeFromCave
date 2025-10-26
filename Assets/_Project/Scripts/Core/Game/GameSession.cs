using System.Collections.Generic;
using UnityEngine;

public sealed class GameSession
{
    private UnitDefinitionSO _heroDifinition;

    private readonly List<UnitDefinitionSO> _armyDefinition = new();

    public UnitDefinitionSO HeroDefinition => _heroDifinition;

    public IReadOnlyList<UnitDefinitionSO> ArmyDefinition => _armyDefinition;

    public void SelectHeroAndArmy(UnitDefinitionSO heroDefinition, List<UnitDefinitionSO> armyDefinition)
    {
        _heroDifinition = heroDefinition;
        _armyDefinition.Clear();
        if (armyDefinition != null)
        {
            _armyDefinition.AddRange(armyDefinition);
        }

        Debug.Log($"[GameSession] Selection updated. Hero: {_heroDifinition?.name ?? "<null>"}, Army size: {_armyDefinition.Count}.");
    }

    public void Clear()
    {
        _heroDifinition = null;
        _armyDefinition.Clear();
        Debug.Log("[GameSession] Selection cleared.");
    }
}
