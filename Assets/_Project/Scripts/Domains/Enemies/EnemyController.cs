using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private UnitDefinitionSO _unitDefinition;

    private UnitModel _unitModel;

    public void Awake()
    {
        _unitModel = new UnitModel(_unitDefinition, 0, 0);
    }

    public UnitStatsModel GetEnemyStats()
    {
        return _unitModel.GetStats();
    }

    public UnitModel GetEnemyModel()
    {
        return _unitModel;
    }
}
