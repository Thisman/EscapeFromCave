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
                return Mathf.FloorToInt(experience / 100f);
            default:
                return 0;
        }
    }
}
