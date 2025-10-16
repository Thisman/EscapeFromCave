using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private UnitDefinitionSO _unitDefinition;
    
    private UnitModel _unitModel;

    public void Awake()
    {
        _unitModel = new UnitModel(_unitDefinition, 0, 0);
    }

    public UnitStatsModel GetPlayerStats()
    {
        return _unitModel.GetStats();
    }

    public UnitModel GetPlayerModel()
    {
        return _unitModel;
    }
}
