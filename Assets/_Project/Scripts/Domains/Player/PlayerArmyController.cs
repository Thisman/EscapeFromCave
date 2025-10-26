using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArmyController : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxSlots = 3;

    private ArmyModel _army;

    public event Action<IReadOnlyArmyModel> ArmyChanged;

    public int MaxSlots => Mathf.Max(1, _maxSlots);

    public IReadOnlyArmyModel Army => _army;

    private void OnDestroy()
    {
        if (_army != null)
        {
            _army.Changed -= HandleArmyChanged;
        }
    }

    public void Initialize(ArmyModel armyModel)
    {
        if (_army != null)
        {
            _army.Changed -= HandleArmyChanged;
        }

        _army = armyModel ?? throw new ArgumentNullException(nameof(armyModel));
        _army.Changed += HandleArmyChanged;
        HandleArmyChanged(_army);
    }

    private void HandleArmyChanged(IReadOnlyArmyModel army)
    {
        ArmyChanged?.Invoke(army);
    }
}
