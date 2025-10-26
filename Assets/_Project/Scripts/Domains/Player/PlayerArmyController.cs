using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArmyController : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxSlots = 3;

    private ArmyModel _army;

    public IReadOnlyArmyModel Army => _army;

    public event Action<IReadOnlyArmyModel> ArmyChanged;

    public int MaxSlots => Mathf.Max(1, _maxSlots);

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

    public bool TryAddSquad(UnitDefinitionSO def, int amount) => _army.TryAddSquad(def, amount);

    public int RemoveSquad(UnitDefinitionSO def, int amount) => _army.RemoveSquad(def, amount);

    public bool TrySplit(UnitDefinitionSO def, int amount) => _army.TrySplit(def, amount);

    public bool TryMerge(int fromIndex, int toIndex) => _army.TryMerge(fromIndex, toIndex);

    public int GetTotal(UnitDefinitionSO def) => _army.GetTotalUnitsInSquad(def);

    public IReadOnlyList<IReadOnlySquadModel> GetSquads() => _army.GetAllSlots();

    public IReadOnlySquadModel GetSlot(int index) => _army.GetSlot(index);

    public bool SetSlot(int index, SquadModel squad) => _army.SetSlot(index, squad);

    public bool ClearSlot(int index) => _army.ClearSlot(index);

    public bool SwapSlots(int a, int b) => _army.SwapSlots(a, b);

    private void OnDestroy()
    {
        if (_army != null)
        {
            _army.Changed -= HandleArmyChanged;
        }
    }

    private void HandleArmyChanged(IReadOnlyArmyModel army)
    {
        ArmyChanged?.Invoke(army);
    }
}
