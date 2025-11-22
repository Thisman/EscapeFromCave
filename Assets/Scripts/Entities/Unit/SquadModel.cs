using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SquadModel : IReadOnlySquadModel
{
    [SerializeField] private UnitSO _unitDefinition;
    [SerializeField, Min(0)] private int _count;
    [SerializeField, Min(1)] private int _level = 1;
    [SerializeField, Min(0f)] private float _experience;

    private readonly Dictionary<SquadUpgradeStat, float> _statBonuses = new();

    public event Action<IReadOnlySquadModel> Changed;

    public event Action<IReadOnlySquadModel> LevelChanged;

    public UnitSO Definition => _unitDefinition;

    public SquadModel(UnitSO definition, int initialCount = 0, float initialExperience = 0f)
    {
        _unitDefinition = definition;
        _count = initialCount;
        _level = 1;
        _experience = Mathf.Max(0f, initialExperience);
        UpdateLevelFromExperience();
    }

    public int Count => _count;

    public bool IsEmpty => _count <= 0;

    public int Level => Mathf.Max(1, _level);

    public float Experience => _experience;

    public float ExperienceToNextLevel
    {
        get
        {
            float nextLevelThreshold = GetExperienceThresholdForLevel(Level + 1);
            return Mathf.Max(0f, nextLevelThreshold - _experience);
        }
    }

    public Sprite Icon => _unitDefinition.Icon;

    public string UnitName => _unitDefinition.UnitName;

    public UnitKind Kind => _unitDefinition.Kind;

    public AttackKind AttackKind => _unitDefinition.AttackKind;

    public DamageType DamageType => _unitDefinition.DamageType;

    public float Health => ApplyBonus(SquadUpgradeStat.Health, CalculateProgressiveStat(_unitDefinition?.BaseHealth ?? 0f, template => template.BaseHealth));

    public float PhysicalDefense => ApplyBonus(SquadUpgradeStat.PhysicalDefense, CalculateProgressiveStat(_unitDefinition?.BasePhysicalDefense ?? 0f, template => template.BasePhysicalDefense));

    public float MagicDefense => ApplyBonus(SquadUpgradeStat.MagicDefense, CalculateProgressiveStat(_unitDefinition?.BaseMagicDefense ?? 0f, template => template.BaseMagicDefense));

    public float AbsoluteDefense => ApplyBonus(SquadUpgradeStat.AbsoluteDefense, CalculateProgressiveStat(_unitDefinition?.BaseAbsoluteDefense ?? 0f, template => template.BaseAbsoluteDefense));

    public (float min, float max) GetBaseDamageRange()
    {
        float minDamage = ApplyBonus(SquadUpgradeStat.MinDamage, CalculateProgressiveStat(_unitDefinition?.MinDamage ?? 0f, template => template.MinDamage));
        float maxDamage = ApplyBonus(SquadUpgradeStat.MaxDamage, CalculateProgressiveStat(_unitDefinition?.MaxDamage ?? 0f, template => template.MaxDamage));
        return (minDamage, maxDamage);
    }

    public float Speed => ApplyBonus(SquadUpgradeStat.Speed, CalculateProgressiveStat(_unitDefinition?.Speed ?? 0f, template => template.Speed));

    public float Initiative => Speed;

    public float CritChance => ApplyBonus(SquadUpgradeStat.CritChance, CalculateProgressiveStat(_unitDefinition?.BaseCritChance ?? 0f, template => template.BaseCritChance));

    public float CritMultiplier => ApplyBonus(SquadUpgradeStat.CritMultiplier, CalculateProgressiveStat(_unitDefinition?.BaseCritMultiplier ?? 0f, template => template.BaseCritMultiplier));

    public float MissChance => ApplyBonus(SquadUpgradeStat.MissChance, CalculateProgressiveStat(_unitDefinition?.BaseMissChance ?? 0f, template => template.BaseMissChance));

    public BattleAbilitySO[] Abilities => _unitDefinition.Abilities;

    public bool IsFriendly () => _unitDefinition.IsFriendly();

    public bool IsAlly () => _unitDefinition.IsAlly();

    public bool IsHero () => _unitDefinition.IsHero();

    public bool IsEnemy () => _unitDefinition.IsEnemy();

    public bool IsNeutral () => _unitDefinition.IsNeutral();

    public bool TryAddUnits(int amount)
    {
        if (amount <= 0) return false;

        _count += amount;
        NotifyChanged();
        return true;
    }

    public bool TryAddExperience(float amount)
    {
        if (amount <= 0f)
            return false;

        _experience += amount;
        UpdateLevelFromExperience();
        NotifyChanged();
        return true;
    }

    public bool TrySetExperience(float experience)
    {
        experience = Mathf.Max(0f, experience);

        if (Mathf.Approximately(_experience, experience))
            return false;

        _experience = experience;
        UpdateLevelFromExperience();
        NotifyChanged();
        return true;
    }

    public void Clear()
    {
        if (_count == 0) return;

        _count = 0;
        NotifyChanged();
    }

    public void ApplyUpgrade(UpgradeModel upgrade)
    {
        if (upgrade == null)
            throw new ArgumentNullException(nameof(upgrade));

        ApplyUpgradeModifiers(upgrade.Modifiers);
    }

    public void ApplyUpgradeModifiers(IEnumerable<SquadUpgradeModifier> modifiers)
    {
        if (modifiers == null)
            return;

        foreach (var modifier in modifiers)
        {
            if (_statBonuses.TryGetValue(modifier.Stat, out float currentValue))
                _statBonuses[modifier.Stat] = currentValue + modifier.Value;
            else
                _statBonuses.Add(modifier.Stat, modifier.Value);
        }

        NotifyChanged();
    }

    private float CalculateProgressiveStat(float baseValue, Func<UnitProgressionTemplateSO, float> progressionSelector)
    {
        if (_unitDefinition == null)
            return baseValue;

        int effectiveLevel = Level;
        if (effectiveLevel <= 1 || progressionSelector == null)
            return baseValue;

        UnitProgressionTemplateSO template = _unitDefinition.ProgressionTemplate;
        if (template == null)
            return baseValue;

        float increment = progressionSelector(template);
        if (Mathf.Approximately(increment, 0f))
            return baseValue;

        return baseValue + increment * (effectiveLevel - 1);
    }

    private float GetExperienceThresholdForLevel(int level)
    {
        if (_unitDefinition == null)
            return 0f;

        return _unitDefinition.LevelExpFunction.GetExperienceForLevel(level);
    }

    private bool UpdateLevelFromExperience()
    {
        int newLevel = _unitDefinition != null
            ? _unitDefinition.LevelExpFunction.CalculateLevel(_experience)
            : Mathf.Max(1, _level);

        newLevel = Mathf.Max(1, newLevel);

        if (newLevel == _level)
            return false;

        _level = newLevel;
        NotifyLevelChanged();
        return true;
    }

    private float ApplyBonus(SquadUpgradeStat stat, float baseValue)
    {
        if (_statBonuses.TryGetValue(stat, out float bonus))
            return baseValue + bonus;

        return baseValue;
    }

    private void NotifyLevelChanged()
    {
        LevelChanged?.Invoke(this);
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
