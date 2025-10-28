using System;
using UnityEngine;

public sealed class BattleSquadModel : IReadOnlySquadModel
{
    private readonly IReadOnlySquadModel _sourceModel;

    private int _squadHealth;
    
    public UnitDefinitionSO Definition => _sourceModel.Definition;

    public event Action<IReadOnlySquadModel> Changed;

    public Sprite Icon => _sourceModel.Icon;

    public string UnitName => _sourceModel.UnitName;

    public UnitKind Kind => _sourceModel.Kind;

    public AttackKind AttackKind => _sourceModel.AttackKind;

    public DamageType DamageType => _sourceModel.DamageType;

    public float Health => _sourceModel.Health;

    public float PhysicalDefense => _sourceModel.PhysicalDefense;

    public float MagicDefense => _sourceModel.MagicDefense;

    public float AbsoluteDefense => _sourceModel.AbsoluteDefense;

    public (float min, float max) GetBaseDamageRange()
    {
        return _sourceModel.GetBaseDamageRange();
    }

    public float Speed => _sourceModel.Speed;

    public float Initiative => _sourceModel.Speed;

    public float CritChance => _sourceModel.CritChance;

    public float CritMultiplier => _sourceModel.CritMultiplier;

    public float MissChance => _sourceModel.MissChance;

    public BattleAbilityDefinitionSO[] Abilities => _sourceModel.Abilities;

    public bool IsFriendly() => _sourceModel.IsFriendly();

    public bool IsAlly() => _sourceModel.IsAlly();

    public bool IsHero() => _sourceModel.IsHero();

    public bool IsEnemy() => _sourceModel.IsEnemy();

    public bool IsNeutral() => _sourceModel.IsNeutral();

    public BattleSquadModel(IReadOnlySquadModel sourceModel)
    {
        _sourceModel = sourceModel ?? throw new ArgumentNullException(nameof(sourceModel));
        _squadHealth = CalculateInitialTotalHealth();
    }

    public int Count => CalculateCount();

    public bool IsEmpty => Count <= 0;

    public void ApplyDamage(int damage)
    {
        if (damage <= 0)
            return;

        if (_squadHealth <= 0)
            return;

        SetSquadHealth(Math.Max(0, _squadHealth - damage));
    }

    private int CalculateInitialTotalHealth()
    {
        return _sourceModel.Count * (int)_sourceModel.Definition.BaseHealth;
    }

    private int CalculateCount()
    {
        int unitBaseHealth = (int)_sourceModel.Definition.BaseHealth;
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
}
