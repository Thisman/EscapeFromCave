using System.Collections.Generic;
using System;
using UnityEngine;

public enum UnitType
{
    Hero,
    Ally,
    Enemy,
    Neutral
}

[CreateAssetMenu(menuName = "RPG/Unit")]
public sealed class UnitDefinitionSO : ScriptableObject
{
    public Sprite Icon;

    public string UnitName;

    public UnitType Type = UnitType.Neutral;

    public List<UnitLevelDefintion> Levels = new();

    public UnitLevelDefintion GetStatsForLevel(int level)
    {
        if (Levels == null || Levels.Count == 0)
        {
            Debug.LogWarning($"{name}: нет данных уровней!");
            return new UnitLevelDefintion();
        }

        int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
        return Levels[index];
    }

    public int GetXPForNextLevel(int level)
    {
        var stats = GetStatsForLevel(level);
        return stats.XPToNext;
    }
}

[Serializable]
public struct UnitLevelDefintion
{
    [Min(0)] public int LevelIndex;

    [Min(0)] public int XPToNext;

    [Min(0)] public int Health;
    [Min(0)] public int Damage;
    [Min(0)] public int Defense;
    [Min(0)] public int Initiative;

    [Min(0)] public float Speed;
}
