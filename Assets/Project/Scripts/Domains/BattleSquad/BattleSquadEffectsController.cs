using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class BattleSquadEffectsController: MonoBehaviour
{
    private readonly List<BattleEffectDefinitionSO> _effects = new();

    public IReadOnlyList<BattleEffectDefinitionSO> Effects => _effects;

    public void AddEffect(BattleEffectDefinitionSO effect)
    {
        _effects.Add(effect);
    }

    public void RemoveEffect(BattleEffectDefinitionSO effect)
    {
        _effects.Remove(effect);
    }
}
