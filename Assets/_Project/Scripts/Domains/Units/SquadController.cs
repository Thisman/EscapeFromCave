using UnityEngine;

public class SquadController : MonoBehaviour
{
    [SerializeField] private UnitDefinitionSO _unitDefinition;

    private UnitModel _unitModel;

    public void Awake()
    {
        _unitModel = new UnitModel(_unitDefinition, 0, 0);
    }

    public UnitStatsModel GetUnitStats()
    {
        return _unitModel.GetStats();
    }

    public IReadOnlyUnitModel GetUnitModel()
    {
        return _unitModel;
    }
}
