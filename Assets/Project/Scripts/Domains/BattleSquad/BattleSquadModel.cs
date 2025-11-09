using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleSquadModel : IReadOnlySquadModel
{
    private int _squadHealth;
    private readonly IReadOnlySquadModel _sourceModel;
    private readonly Dictionary<object, Dictionary<BattleSquadStat, float>> _statModifiersBySource = new();
    private readonly Dictionary<BattleSquadStat, float> _aggregatedModifiers = new();

    public UnitDefinitionSO Definition => _sourceModel.Definition;

    public event Action<IReadOnlySquadModel> Changed;

    public BattleSquadModel(IReadOnlySquadModel sourceModel)
    {
        _sourceModel = sourceModel ?? throw new ArgumentNullException(nameof(sourceModel));
        _squadHealth = CalculateInitialTotalHealth();
    }

    public Sprite Icon => _sourceModel.Icon;

    public string UnitName => _sourceModel.UnitName;

    public UnitKind Kind => _sourceModel.Kind;

    public AttackKind AttackKind => _sourceModel.AttackKind;

    public DamageType DamageType => _sourceModel.DamageType;

    public float Health => GetModifiedStat(BattleSquadStat.Health, _sourceModel.Health);

    public float PhysicalDefense => GetModifiedStat(BattleSquadStat.PhysicalDefense, _sourceModel.PhysicalDefense);

    public float MagicDefense => GetModifiedStat(BattleSquadStat.MagicDefense, _sourceModel.MagicDefense);

    public float AbsoluteDefense => GetModifiedStat(BattleSquadStat.AbsoluteDefense, _sourceModel.AbsoluteDefense);

    public (float min, float max) GetBaseDamageRange()
    {
        var (min, max) = _sourceModel.GetBaseDamageRange();
        min = GetModifiedStat(BattleSquadStat.MinDamage, min);
        max = GetModifiedStat(BattleSquadStat.MaxDamage, max);
        return (min, max);
    }

    public float Speed => GetModifiedStat(BattleSquadStat.Speed, _sourceModel.Speed);

    public float Initiative => GetModifiedStat(BattleSquadStat.Initiative, _sourceModel.Initiative);

    public float CritChance => GetModifiedStat(BattleSquadStat.CritChance, _sourceModel.CritChance);

    public float CritMultiplier => GetModifiedStat(BattleSquadStat.CritMultiplier, _sourceModel.CritMultiplier);

    public float MissChance => GetModifiedStat(BattleSquadStat.MissChance, _sourceModel.MissChance);

    public BattleAbilityDefinitionSO[] Abilities => _sourceModel.Abilities;

    public bool IsFriendly() => _sourceModel.IsFriendly();

    public bool IsAlly() => _sourceModel.IsAlly();

    public bool IsHero() => _sourceModel.IsHero();

    public bool IsEnemy() => _sourceModel.IsEnemy();

    public bool IsNeutral() => _sourceModel.IsNeutral();

    public int Count => CalculateCount();

    public bool IsEmpty => Count <= 0;

    public bool ApplyDamage(BattleDamageData damageData)
    {
        if (damageData == null)
            return false;

        int damage = damageData.Value;
        if (damage <= 0 || _squadHealth <= 0) return false;

        // 1) Промах атакующего (магический урон не может промахнуться)
        bool canMiss = damageData.DamageType != DamageType.Magical;
        if (canMiss)
        {
            float pMiss = Mathf.Clamp01(MissChance);
            if (UnityEngine.Random.value < pMiss)
            {
                GameLogger.Log($"[Battle]: {UnitName} dodge damage");
                return false;
            }
        }

        // 2) Защита от урона в зависимости от его типа (0..1)
        float defense = Mathf.Clamp01(AbsoluteDefense);

        switch (damageData.DamageType)
        {
            case DamageType.Physical:
                defense += Mathf.Clamp01(PhysicalDefense);
                break;
            case DamageType.Magical:
                defense += Mathf.Clamp01(MagicDefense);
                break;
        }

        defense = Mathf.Clamp01(defense);

        // Если защита хранится в процентах (0..100), конвертируй каждую составляющую в диапазон 0..1 перед суммированием.

        // 3) Применяем процент ко всему входящему урону один раз
        //    Вопрос округления: RoundToInt — нейтральный вариант. Если хочешь «не завышать» снижение, используй FloorToInt.
        int afterDefense = Mathf.Max(0, Mathf.RoundToInt(damage * (1f - defense)));

        if (afterDefense <= 0) return false;

        int newHealth = Mathf.Max(0, _squadHealth - afterDefense);

        GameLogger.Log($"[Battle]: {UnitName} took {afterDefense} {damageData.DamageType} dmg (raw={damage}, defense={defense:P0})");
        GameLogger.Log($"[Battle]: {UnitName} new health {newHealth}");

        bool changed = newHealth != _squadHealth;
        SetSquadHealth(newHealth);
        return changed;
    }

    public BattleDamageData ResolveDamage()
    {
        var (minD, maxD) = GetBaseDamageRange();
        if (maxD < minD) (minD, maxD) = (maxD, minD);

        float pCrit = Mathf.Clamp01(CritChance);
        float critMul = Mathf.Max(1f, CritMultiplier);

        int total = 0;
        for (int i = 0; i < Count; i++)
        {
            float dmg = UnityEngine.Random.Range(minD, maxD); // float-версия даёт непрерывный диапазон
            if (UnityEngine.Random.value < pCrit)
                dmg *= critMul;

            total += Mathf.FloorToInt(dmg);
        }

        total = Mathf.Max(0, total);
        GameLogger.Log($"[Battle]: {UnitName} resolve damage {total} of type {DamageType}");
        return new BattleDamageData(DamageType, total);
    }

    public void SetStatModifiers(object source, IReadOnlyList<BattleStatModifier> modifiers)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (modifiers == null || modifiers.Count == 0)
        {
            RemoveStatModifiers(source);
            return;
        }

        if (!_statModifiersBySource.TryGetValue(source, out var existingModifiers))
        {
            existingModifiers = new Dictionary<BattleSquadStat, float>();
            _statModifiersBySource[source] = existingModifiers;
        }
        else if (AreModifiersEqual(existingModifiers, modifiers))
        {
            return;
        }

        bool hadExistingModifiers = existingModifiers.Count > 0;
        if (hadExistingModifiers)
        {
            foreach (var kvp in existingModifiers)
            {
                if (_aggregatedModifiers.TryGetValue(kvp.Key, out var currentValue))
                {
                    var updatedValue = currentValue - kvp.Value;
                    if (Mathf.Approximately(updatedValue, 0f))
                        _aggregatedModifiers.Remove(kvp.Key);
                    else
                        _aggregatedModifiers[kvp.Key] = updatedValue;
                }
            }

            existingModifiers.Clear();
        }

        bool appliedAnyModifier = false;
        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            if (Mathf.Approximately(modifier.Value, 0f))
                continue;

            existingModifiers[modifier.Stat] = modifier.Value;
            if (_aggregatedModifiers.TryGetValue(modifier.Stat, out var currentValue))
                _aggregatedModifiers[modifier.Stat] = currentValue + modifier.Value;
            else
                _aggregatedModifiers[modifier.Stat] = modifier.Value;

            appliedAnyModifier = true;
        }

        if (!appliedAnyModifier)
        {
            _statModifiersBySource.Remove(source);
        }

        if (hadExistingModifiers || appliedAnyModifier)
        {
            NotifyChanged();
        }
    }

    public void RemoveStatModifiers(object source)
    {
        if (source == null)
            return;

        if (!_statModifiersBySource.TryGetValue(source, out var existingModifiers) || existingModifiers.Count == 0)
            return;

        bool changed = false;
        foreach (var kvp in existingModifiers)
        {
            if (_aggregatedModifiers.TryGetValue(kvp.Key, out var currentValue))
            {
                var updatedValue = currentValue - kvp.Value;
                if (Mathf.Approximately(updatedValue, 0f))
                    _aggregatedModifiers.Remove(kvp.Key);
                else
                    _aggregatedModifiers[kvp.Key] = updatedValue;

                changed = true;
            }
        }

        _statModifiersBySource.Remove(source);

        if (changed)
        {
            NotifyChanged();
        }
    }

    private int CalculateInitialTotalHealth()
    {
        return _sourceModel.Count * (int)_sourceModel.Health;
    }

    private int CalculateCount()
    {
        int unitBaseHealth = (int)_sourceModel.Health;
        return (_squadHealth + unitBaseHealth - 1) / unitBaseHealth;
    }

    private void SetSquadHealth(int newSquadHealth)
    {
        newSquadHealth = Math.Max(0, newSquadHealth);
        if (_squadHealth == newSquadHealth)
            return;

        _squadHealth = newSquadHealth;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }

    private static bool AreModifiersEqual(Dictionary<BattleSquadStat, float> existingModifiers, IReadOnlyList<BattleStatModifier> modifiers)
    {
        int nonZeroCount = 0;
        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            if (Mathf.Approximately(modifier.Value, 0f))
                continue;

            nonZeroCount++;
            if (!existingModifiers.TryGetValue(modifier.Stat, out var existingValue) || !Mathf.Approximately(existingValue, modifier.Value))
                return false;
        }

        return existingModifiers.Count == nonZeroCount;
    }

    private float GetModifiedStat(BattleSquadStat stat, float baseValue)
    {
        if (_aggregatedModifiers.TryGetValue(stat, out var modifier))
            return baseValue + modifier;

        return baseValue;
    }
}
