using UnityEngine;
using VContainer;

public class PlayerController : MonoBehaviour
{
    private UnitModel _unitModel;

    [Inject] private IGameSession _gameSession;

    private void Awake()
    {
        _unitModel = new UnitModel(_gameSession.Hero);
    }

    public UnitStatsModel GetPlayerStats()
    {
        if (_unitModel == null)
            return null;

        return _unitModel.GetStats();
    }

    public IReadOnlyUnitModel GetPlayerModel()
    {
        if (_unitModel == null)
            return null;

        return _unitModel;
    }
}
