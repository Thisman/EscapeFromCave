using System.Collections.Generic;
using UnityEngine;

public sealed class GameSession
{
    private UnitDefinitionSO _heroDifinition;

    private readonly List<UnitDefinitionSO> _armyDefinition = new();

    public UnitDefinitionSO HeroDefinition => _heroDifinition;

    public IReadOnlyList<UnitDefinitionSO> ArmyDefinition => _armyDefinition;

    public void SetSelection(UnitDefinitionSO heroDefinition, List<UnitDefinitionSO> armyDefinition)
    {
        if (heroDefinition == null)
        {
            Debug.LogWarning("[GameSession] Setting hero selection with a null hero reference.");
        }

        _heroDifinition = heroDefinition;
        _armyDefinition.Clear();
        if (armyDefinition != null)
        {
            _armyDefinition.AddRange(armyDefinition);
        }

        if (armyDefinition == null || armyDefinition.Count == 0)
        {
            Debug.LogWarning("[GameSession] Army selection is empty.");
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
