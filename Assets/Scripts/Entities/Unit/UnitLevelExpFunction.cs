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
                return Mathf.Max(1, Mathf.FloorToInt(experience / 100f) + 1);
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
                return (level - 1) * 100f;
            default:
                return 0f;
        }
    }
}
