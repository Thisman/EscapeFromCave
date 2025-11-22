using System;
using UnityEngine;
using VContainer;

public class PlayerController : MonoBehaviour
{
    [Inject] private readonly GameEventBusService _sceneEventBusService;

    private SquadModel _squadModel;

    private void OnDestroy()
    {
        if (_squadModel != null)
            _squadModel.LevelChanged -= HandleLevelChanged;
    }

    public void Initialize(SquadModel squadModel)
    {
        _squadModel = squadModel ?? throw new ArgumentNullException(nameof(squadModel));
        _squadModel.LevelChanged += HandleLevelChanged;
    }

    public float GetMovementSpeed()
    {
        return _squadModel.Speed;
    }

    public IReadOnlySquadModel GetPlayer()
    {
        return _squadModel;
    }

    public SquadModel GetSquadModel()
    {
        return _squadModel;
    }

    private void HandleLevelChanged(IReadOnlySquadModel squad)
    {
        _sceneEventBusService?.Publish(new RequestPlayerUpgrade());
    }
}
