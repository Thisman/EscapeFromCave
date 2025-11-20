using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class PlayerArmyController : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxSlots = 3;
    [SerializeField] private PlayerController _playerController;

    [Inject] private readonly GameEventBusService _sceneEventBusService;

    private ArmyModel _army;

    public int MaxSlots => _maxSlots;

    public IReadOnlyArmyModel Army => _army;

    private void OnDestroy()
    {
        _army.Changed -= HandleArmyChanged;
    }

    public void Initialize(ArmyModel armyModel)
    {
        _army = armyModel ?? throw new ArgumentNullException(nameof(armyModel));
        _army.Changed += HandleArmyChanged;
        HandleArmyChanged(_army);
    }

    private void HandleArmyChanged(IReadOnlyArmyModel army)
    {
        List<IReadOnlySquadModel> armyList = new()
        {
            _playerController.GetPlayer()
        };
        armyList.AddRange(army.GetSquads());
        _sceneEventBusService.Publish<PlayerSquadsChanged>(new PlayerSquadsChanged(armyList));
    }
}
