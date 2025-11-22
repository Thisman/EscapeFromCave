using System;
using System.Collections.Generic;

public enum SquadUpgradeStat
{
    Health,
    PhysicalDefense,
    MagicDefense,
    AbsoluteDefense,
    MinDamage,
    MaxDamage,
    Speed,
    CritChance,
    CritMultiplier,
    MissChance
}

public readonly struct SquadUpgradeModifier
{
    public SquadUpgradeStat Stat { get; }

    public float Value { get; }

    public SquadUpgradeModifier(SquadUpgradeStat stat, float value)
    {
        Stat = stat;
        Value = value;
    }
}

public sealed class UpgradeModel
{
    public IReadOnlySquadModel Target { get; }

    public IReadOnlyList<SquadUpgradeModifier> Modifiers { get; }

    public string Description { get; }

    public UpgradeModel(IReadOnlySquadModel target, IReadOnlyList<SquadUpgradeModifier> modifiers, string description)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Modifiers = modifiers ?? throw new ArgumentNullException(nameof(modifiers));
        Description = description ?? string.Empty;
    }

    public void Apply(SquadModel squadModel)
    {
        if (squadModel == null)
            throw new ArgumentNullException(nameof(squadModel));

        squadModel.ApplyUpgradeModifiers(Modifiers);
    }
}
