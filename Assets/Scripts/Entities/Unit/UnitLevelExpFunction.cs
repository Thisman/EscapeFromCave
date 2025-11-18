using UnityEngine;

public enum UnitLevelExpFunction
{
    Linear = 0,
}

public static class UnitLevelExpFunctionExtensions
{
    public static int CalculateLevel(this UnitLevelExpFunction function, float experience)
    {
        experience = Mathf.Max(0f, experience);

        switch (function)
        {
            case UnitLevelExpFunction.Linear:
                const float coefficient = 0.16f; // 4/25
                const float offset = -7f;
                const float constant = 49f;

                var discriminant = Mathf.Sqrt(constant + coefficient * experience);
                var level = Mathf.FloorToInt((offset + discriminant) / 2f) + 1;
                return Mathf.Max(1, level);
            default:
                return 1;
        }
    }

    public static float GetExperienceForLevel(this UnitLevelExpFunction function, int level)
    {
        level = Mathf.Max(1, level);

        switch (function)
        {
            case UnitLevelExpFunction.Linear:
                return 25f * (level - 1) * (level + 6);
            default:
                return 0f;
        }
    }
}
