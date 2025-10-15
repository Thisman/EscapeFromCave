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
    public class UnitDefinition : ScriptableObject
    {
        [Header("�����")]
        public string UnitName;
        public UnitType Type = UnitType.Neutral;

        [Header("������ � �����")]
        [Tooltip("�������� ������ �� ������ ������ (� ������� �����������).")]
        public List<UnitStatsLevel> Levels = new();

        /// <summary>
        /// ���������� ����� �� ����������� ������.
        /// ���� ����������� ������� ��������� ������ � ���� ���������.
        /// </summary>
        public UnitStatsLevel GetStatsForLevel(int level)
        {
            if (Levels == null || Levels.Count == 0)
            {
                Debug.LogWarning($"{name}: ��� ������ �������!");
                return null;
            }

            int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
            return Levels[index];
        }

        /// <summary>
        /// ���������� ��������� XP ��� �������� ������.
        /// ���� ������� ��������� � ���������� 0.
        /// </summary>
        public int GetXPForNextLevel(int level)
        {
            var stats = GetStatsForLevel(level);
            return stats?.XPToNext ?? 0;
        }
    }
}
