using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UnitController
{
    private readonly IReadOnlySquadModel _squadModel;
    private readonly UnitSO _unitDefinition;
    private readonly BattleSquadEffectsController _effectsController;

    public UnitController(UnitSO definition)
    {
        _unitDefinition = definition;
    }

    public UnitController(IReadOnlySquadModel squadModel, BattleSquadEffectsController effectsController = null)
    {
        _squadModel = squadModel;
        _unitDefinition = squadModel?.Definition;
        _effectsController = effectsController;
    }

    public string Title => _squadModel?.UnitName ?? _unitDefinition?.UnitName ?? string.Empty;

    public Sprite Icon => _squadModel?.Icon ?? _unitDefinition?.Icon;

    public IReadOnlyList<BattleAbilitySO> GetAbilities()
    {
        BattleAbilitySO[] squadAbilities = _squadModel?.Abilities;
        if (squadAbilities != null && squadAbilities.Length > 0)
            return squadAbilities;

        BattleAbilitySO[] definitionAbilities = _unitDefinition?.Abilities;
        if (definitionAbilities != null && definitionAbilities.Length > 0)
            return definitionAbilities;

        return Array.Empty<BattleAbilitySO>();
    }

    public IReadOnlyList<BattleEffectSO> GetEffects()
    {
        IReadOnlyList<BattleEffectSO> effects = _effectsController?.Effects;
        if (effects != null && effects.Count > 0)
            return effects;

        return Array.Empty<BattleEffectSO>();
    }

    public int GetCount(int defaultValue = 1)
    {
        if (_squadModel != null)
            return _squadModel.Count;

        return Math.Max(0, defaultValue);
    }

    public float GetHealth()
    {
        if (_squadModel != null)
            return _squadModel.Health;

        return _unitDefinition?.BaseHealth ?? 0f;
    }

    public (float min, float max) GetDamageRange()
    {
        if (_squadModel != null)
            return _squadModel.GetBaseDamageRange();

        if (_unitDefinition != null)
            return _unitDefinition.GetBaseDamageRange();

        return (0f, 0f);
    }

    public float GetInitiative()
    {
        if (_squadModel != null)
            return _squadModel.Initiative;

        return _unitDefinition?.Speed ?? 0f;
    }

    public AttackKind GetAttackKind()
    {
        if (_squadModel != null)
            return _squadModel.AttackKind;

        return _unitDefinition?.AttackKind ?? AttackKind.Melee;
    }

    public DamageType GetDamageType()
    {
        if (_squadModel != null)
            return _squadModel.DamageType;

        return _unitDefinition?.DamageType ?? DamageType.Physical;
    }

    public float GetPhysicalDefense()
    {
        if (_squadModel != null)
            return _squadModel.PhysicalDefense;

        return _unitDefinition?.BasePhysicalDefense ?? 0f;
    }

    public float GetMagicDefense()
    {
        if (_squadModel != null)
            return _squadModel.MagicDefense;

        return _unitDefinition?.BaseMagicDefense ?? 0f;
    }

    public float GetAbsoluteDefense()
    {
        if (_squadModel != null)
            return _squadModel.AbsoluteDefense;

        return _unitDefinition?.BaseAbsoluteDefense ?? 0f;
    }

    public float GetCritChance()
    {
        if (_squadModel != null)
            return _squadModel.CritChance;

        return _unitDefinition?.BaseCritChance ?? 0f;
    }

    public float GetCritMultiplier()
    {
        if (_squadModel != null)
            return _squadModel.CritMultiplier;

        return _unitDefinition?.BaseCritMultiplier ?? 0f;
    }

    public float GetMissChance()
    {
        if (_squadModel != null)
            return _squadModel.MissChance;

        return _unitDefinition?.BaseMissChance ?? 0f;
    }
}
