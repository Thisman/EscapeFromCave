public sealed class BattleDamageData
{
    public BattleDamageData(DamageType damageType, int value)
    {
        DamageType = damageType;
        Value = value;
    }

    public DamageType DamageType { get; }

    public int Value { get; }
}
