using UnityEngine;

public class BattleUnitController : MonoBehaviour
{
    public UnitModel UnitModel;

    public UnitStatsModel GetEnemyStats()
    {
        return UnitModel.GetStats();
    }

    public IReadOnlyUnitModel GetEnemyModel()
    {
        return UnitModel;
    }
}
