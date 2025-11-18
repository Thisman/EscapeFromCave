using System;
using UnityEngine;

public interface IReadOnlySquadModel
{
    public event Action<IReadOnlySquadModel> Changed;

    public event Action<IReadOnlySquadModel> LevelChanged;

    public UnitSO Definition { get; }

    public int Count { get; }

    public int Level { get; }

    public float Experience { get; }

    public float ExperienceToNextLevel { get; }

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

    public BattleAbilitySO[] Abilities { get; }

    public bool IsFriendly ();

    public bool IsAlly ();

    public bool IsHero ();

    public bool IsEnemy ();

    public bool IsNeutral ();
}
