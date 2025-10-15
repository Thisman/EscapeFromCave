using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class UnitStatsLevel
    {
        [Min(1)] public int LevelIndex = 1;

        [Tooltip("������� ����� ����� ��� �������� �� ��������� �������.")]
        [Min(0)] public int XPToNext = 0;

        [Header("�������������� �� ���� ������")]
        [Min(1)] public int Health = 100;
        [Min(0)] public int Damage = 10;
        [Min(0)] public int Defense = 5;
        [Min(0)] public int Initiative = 5;

        [Tooltip("������������ ������ ��� �����. ��� ������� ������ ����� �������� 0.")]
        [Min(0)] public float Speed = 0;
    }
}
