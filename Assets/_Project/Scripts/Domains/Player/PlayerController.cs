using UnityEngine;
using VContainer;

public class PlayerController : MonoBehaviour
{
    private UnitModel _unitModel;

    [Inject] private readonly GameSession _gameSession;

    private void Awake()
    {
        _unitModel = new UnitModel(_gameSession.HeroDefinition);
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
