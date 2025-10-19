using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class PlayerArmyController : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxSlots = 3;

    [Inject] private GameSession _gameSession;

    private ArmyModel _army;

    public IReadOnlyArmyModel Army => _army;
    public event Action<IReadOnlyArmyModel> ArmyChanged;

    private void Start()
    {
        _army = new ArmyModel(_maxSlots);
        for (int i = 0; i < _gameSession.ArmyDefinition.Count; i++)
        {
            var def = _gameSession.ArmyDefinition[i];
            if (def != null)
                TryAddUnits(def, 10);
        }

        _army.Changed += army => ArmyChanged?.Invoke(army);
    }

    public bool TryAddUnits(UnitDefinitionSO def, int amount) => _army.TryAddUnits(def, amount);
    public int RemoveUnits(UnitDefinitionSO def, int amount) => _army.RemoveUnits(def, amount);
    public bool TrySplit(UnitDefinitionSO def, int amount) => _army.TrySplit(def, amount);
    public bool TryMerge(int fromIndex, int toIndex) => _army.TryMerge(fromIndex, toIndex);

    public int GetTotal(UnitDefinitionSO def) => _army.GetTotalUnits(def);
    public IReadOnlyList<IReadOnlySquadModel> GetSquads() => _army.GetAllSlots();
    public IReadOnlySquadModel GetSlot(int index) => _army.GetSlot(index);
    public bool SetSlot(int index, SquadModel squad) => _army.SetSlot(index, squad);
    public bool ClearSlot(int index) => _army.ClearSlot(index);
    public bool SwapSlots(int a, int b) => _army.SwapSlots(a, b);
}
