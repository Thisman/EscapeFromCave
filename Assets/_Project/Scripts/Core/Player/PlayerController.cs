using Game.Data;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private UnitDefinition _unitDefinition;
    
    private UnitModel _unitModel;

    public void Start()
    {
        _unitModel = new UnitModel(_unitDefinition, 0, 0);
    }

    public UnitStatsModel GetPlayerStats()
    {
        return _unitModel.GetStats();
    }
}
