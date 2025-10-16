using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
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
        public Sprite Sprite;

        [Header("Уровни и статы")]
        [Tooltip("Описание статов на каждом уровне (в порядке возрастания).")]
        public List<UnitStatsLevel> Levels = new();

        public UnitStatsLevel GetStatsForLevel(int level)
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
}
