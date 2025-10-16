using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class UnitStatsLevel
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
}
