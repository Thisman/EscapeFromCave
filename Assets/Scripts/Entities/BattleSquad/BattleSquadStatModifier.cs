using System;

[Serializable]
public struct BattleSquadStatModifier
{
    public BattleSquadStat Stat;
    public float Value;

    public BattleSquadStatModifier(BattleSquadStat stat, float value)
    {
        Stat = stat;
        Value = value;
    }
}
