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
public class UnitDefinitionSO : ScriptableObject, IUnitDefinition
{
    [Header("Общее")]
    public string UnitName;
    public UnitType Type = UnitType.Neutral;
    public Sprite Icon;

    [Header("Уровни и статы")]
    [Tooltip("Описание статов на каждом уровне (в порядке возрастания).")]
    public List<UnitStatsLevel> Levels = new();

    public string Id => name;

    public UnitStatsModel GetStatsForLevel(int level)
    {
        var stats = GetLevelData(level);
        return stats?.ToModel(level);
    }

    public int GetXPForNextLevel(int level)
    {
        var stats = GetLevelData(level);
        return stats?.XPToNext ?? 0;
    }

    private UnitStatsLevel GetLevelData(int level)
    {
        if (Levels == null || Levels.Count == 0)
        {
            Debug.LogWarning($"{name}: нет данных уровней!");
            return null;
        }

        int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
        return Levels[index];
    }
}
