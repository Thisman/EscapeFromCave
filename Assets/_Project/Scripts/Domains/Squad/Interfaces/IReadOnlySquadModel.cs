using System;
using UnityEngine;

public interface IReadOnlySquadModel
{
    public UnitDefinitionSO Definition { get; }

    public event Action<IReadOnlySquadModel> Changed;

    public int Count { get; }

    public bool IsEmpty { get; }

    public Sprite Icon { get; }

    public string UnitName { get; }

    public UnitKind Kind { get; }

    public AttackKind AttackKind { get; }

    public DamageType DamageType { get; }

    public float Health { get; }

    public float PhysicalDefense { get; }

    public float MagicDefense { get; }

    public float AbsoluteDefense { get; }

    public (float min, float max) GetBaseDamageRange();

    public float Speed { get; }

    public float Initiative { get; }

    public float CritChance { get; }

    public float CritMultiplier { get; }

    public float MissChance { get; }

    public BattleAbilityDefinitionSO[] Abilities { get; }

    public bool IsFriendly ();

    public bool IsAlly ();

    public bool IsHero ();

    public bool IsEnemy ();

    public bool IsNeutral ();
}
