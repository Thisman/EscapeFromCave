using UnityEngine;

public interface IReadOnlyUnitStatsModel
{
    public int Level { get; }

    public int Health { get; }

    public int Damage { get; }

    public int Defense { get; }

    public int Initiative { get; }

    public float Speed { get; }

    public int XPToNext { get; }
}
