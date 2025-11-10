using System;
using UnityEngine;

[Serializable]
public class SquadModel : IReadOnlySquadModel
{
    [SerializeField] private UnitSO _unitDefinition;
    [SerializeField, Min(0)] private int _count;

    public UnitSO Definition => _unitDefinition;

    public event Action<IReadOnlySquadModel> Changed;

    public SquadModel(UnitSO definition, int initialCount = 0)
    {
        _unitDefinition = definition;
        _count = initialCount;
    }

    public int Count => _count;

    public bool IsEmpty => _count <= 0;

    public Sprite Icon => _unitDefinition.Icon;

    public string UnitName => _unitDefinition.UnitName;

    public UnitKind Kind => _unitDefinition.Kind;

    public AttackKind AttackKind => _unitDefinition.AttackKind;

    public DamageType DamageType => _unitDefinition.DamageType;

    public float Health => _unitDefinition.BaseHealth;

    public float PhysicalDefense => _unitDefinition.BasePhysicalDefense;

    public  float MagicDefense => _unitDefinition.BaseMagicDefense;

    public  float AbsoluteDefense => _unitDefinition.BaseAbsoluteDefense;

    public (float min, float max) GetBaseDamageRange()
    {
        return (_unitDefinition.MinDamage, _unitDefinition.MaxDamage);
    }

    public float Speed => _unitDefinition.Speed;

    public float Initiative => _unitDefinition.Speed;

    public float CritChance => _unitDefinition.BaseCritChance;

    public float CritMultiplier => _unitDefinition.BaseCritMultiplier;

    public float MissChance => _unitDefinition.BaseMissChance;

    public BattleAbilitySO[] Abilities => _unitDefinition.Abilities;

    public bool IsFriendly () => _unitDefinition.IsFriendly();

    public bool IsAlly () => _unitDefinition.IsAlly();

    public bool IsHero () => _unitDefinition.IsHero();

    public bool IsEnemy () => _unitDefinition.IsEnemy();

    public bool IsNeutral () => _unitDefinition.IsNeutral();

    public bool TryAdd(int amount)
    {
        if (amount <= 0) return false;

        _count += amount;
        NotifyChanged();
        return true;
    }

    public void Clear()
    {
        if (_count == 0) return;

        _count = 0;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
