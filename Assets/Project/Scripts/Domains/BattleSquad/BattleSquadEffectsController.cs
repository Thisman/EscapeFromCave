using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class BattleSquadEffectsController: MonoBehaviour
{
    private readonly List<BattleEffectSO> _effects = new();

    public IReadOnlyList<BattleEffectSO> Effects => _effects;

    public void AddEffect(BattleEffectSO effect)
    {
        _effects.Add(effect);
    }

    public void RemoveEffect(BattleEffectSO effect)
    {
        _effects.Remove(effect);
    }
}
