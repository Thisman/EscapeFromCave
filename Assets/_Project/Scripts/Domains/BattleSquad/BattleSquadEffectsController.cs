using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleSquadEffectsController : MonoBehaviour
{
    private readonly List<IReadonlyBattleEffectModel> _effects = new();

    public IReadOnlyList<IReadonlyBattleEffectModel> Effects => _effects;

    public event Action<IReadonlyBattleEffectModel> EffectAdded;

    public event Action<IReadonlyBattleEffectModel> EffectRemoved;

    private void OnDestroy()
    {
        _effects.Clear();
    }

    public bool ContainsEffect(IReadonlyBattleEffectModel effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        return _effects.Contains(effect);
    }

    public void AddEffect(IReadonlyBattleEffectModel effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        if (_effects.Contains(effect))
            return;

        _effects.Add(effect);
        EffectAdded?.Invoke(effect);
    }

    public bool RemoveEffect(IReadonlyBattleEffectModel effect)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        if (!_effects.Remove(effect))
            return false;

        EffectRemoved?.Invoke(effect);
        return true;
    }
}
