using System;
using UnityEngine;
using VContainer;

public class PlayerUpgradeController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;

    [Inject] private readonly GameEventBusService _sceneEventBusService;

    private IDisposable _upgradeSubscription;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (_sceneEventBusService != null)
            _upgradeSubscription = _sceneEventBusService.Subscribe<SelectSquadUpgrade>(HandleSelectUpgrade);
    }

    private void Unsubscribe()
    {
        _upgradeSubscription?.Dispose();
        _upgradeSubscription = null;
    }

    private void HandleSelectUpgrade(SelectSquadUpgrade evt)
    {
        if (evt?.Upgrade == null)
            return;

        var targetModel = ResolveTarget(evt.Upgrade.Target);
        if (targetModel != null)
            evt.Upgrade.Apply(targetModel);
    }

    private SquadModel ResolveTarget(IReadOnlySquadModel target)
    {
        if (target is SquadModel squadModel)
            return squadModel;

        if (_playerController != null && ReferenceEquals(_playerController.GetPlayer(), target))
            return _playerController.GetSquadModel();

        return null;
    }
}
