using System.Collections.Generic;
using UnityEngine;

public sealed class GameSession : IGameSession
{
    private UnitDefinitionSO _hero;

    private readonly List<UnitDefinitionSO> _army = new();

    public UnitDefinitionSO Hero => _hero;

    public IReadOnlyList<UnitDefinitionSO> Army => _army;

    public bool HasSelection => _hero != null && _army.Count > 0;

    public void SetSelection(UnitDefinitionSO hero, List<UnitDefinitionSO> army)
    {
        if (hero == null)
        {
            Debug.LogWarning("[GameSession] Setting hero selection with a null hero reference.");
        }

        _hero = hero;
        _army.Clear();
        if (army != null)
        {
            _army.AddRange(army);
        }

        if (army == null || army.Count == 0)
        {
            Debug.LogWarning("[GameSession] Army selection is empty.");
        }

        Debug.Log($"[GameSession] Selection updated. Hero: {_hero?.name ?? "<null>"}, Army size: {_army.Count}.");
    }

    public void Clear()
    {
        _hero = null;
        _army.Clear();
        Debug.Log("[GameSession] Selection cleared.");
    }
}
