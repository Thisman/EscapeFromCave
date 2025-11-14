using System;

[Serializable]
public struct BattleStatModifier
{
    public BattleSquadStat Stat;
    public float Value;

    public BattleStatModifier(BattleSquadStat stat, float value)
    {
        Stat = stat;
        Value = value;
    }
}
