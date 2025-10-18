using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Hero,
    Ally,
    Enemy,
    Neutral
}

[CreateAssetMenu(menuName = "RPG/Unit Definition", fileName = "UD_NewUnit")]
public class UnitDefinitionSO : ScriptableObject
{
    [Header("Общее")]
    public string UnitName;
    public UnitType Type = UnitType.Neutral;
    public Sprite Icon;

    [Header("Уровни и статы")]
    [Tooltip("Описание статов на каждом уровне (в порядке возрастания).")]
    public List<UnitStatsLevelDifinition> Levels = new();

    public UnitStatsLevelDifinition GetStatsForLevel(int level)
    {
        if (Levels == null || Levels.Count == 0)
        {
            Debug.LogWarning($"{name}: нет данных уровней!");
            return null;
        }

        int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
        return Levels[index];
    }

    public int GetXPForNextLevel(int level)
    {
        var stats = GetStatsForLevel(level);
        return stats?.XPToNext ?? 0;
    }
}

[System.Serializable]
public class UnitStatsLevelDifinition
{
    [Min(1)] public int LevelIndex = 1;

    [Tooltip("Сколько опыта нужно для перехода на следующий уровень.")]
    [Min(0)] public int XPToNext = 0;

    [Header("Характеристики на этом уровне")]
    [Min(1)] public int Health = 100;
    [Min(0)] public int Damage = 10;
    [Min(0)] public int Defense = 5;
    [Min(0)] public int Initiative = 5;

    [Tooltip("Используется только для героя. Для обычных юнитов можно оставить 0.")]
    [Min(0)] public float Speed = 0;
}
