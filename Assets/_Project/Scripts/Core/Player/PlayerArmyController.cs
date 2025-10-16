using System;
using UnityEngine;

public class PlayerArmyController : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxSlots = 3;

    public ArmyModel Army { get; private set; }
    public event Action ArmyChanged;

    public UnitDefinitionSO[] Debug_HeroArmy;

    private void Awake()
    {
        Army = new ArmyModel(_maxSlots);
        Army.Changed += () => ArmyChanged?.Invoke();

        for(int i = 0; i < Debug_HeroArmy.Length; i++)
        {
            if (Debug_HeroArmy[i] != null)
                Army.TryAddUnits(Debug_HeroArmy[i], 10);
        }
    }

    public bool TryAddUnits(UnitDefinitionSO def, int amount) => Army.TryAddUnits(def, amount);
    public int RemoveUnits(UnitDefinitionSO def, int amount) => Army.RemoveUnits(def, amount);
    public bool TrySplit(UnitDefinitionSO def, int amount) => Army.TrySplit(def, amount);
    public bool TryMerge(int fromIndex, int toIndex) => Army.TryMerge(fromIndex, toIndex);

    public int GetTotal(UnitDefinitionSO def) => Army.GetTotalUnits(def);
    public SquadModel GetSlot(int index) => Army.GetSlot(index);
    public bool SetSlot(int index, SquadModel squad) => Army.SetSlot(index, squad);
    public bool ClearSlot(int index) => Army.ClearSlot(index);
    public bool SwapSlots(int a, int b) => Army.SwapSlots(a, b);
}
