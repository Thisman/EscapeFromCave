using UnityEngine;

public class BattleUnitPresenter : MonoBehaviour
{
    public IReadOnlyUnitModel Unit { get; private set; }
    public IReadOnlySquadModel Squad { get; private set; }

    public void Initialize(IReadOnlyUnitModel unit)
    {
        Unit = unit;
        Squad = null;
        var unitName = unit?.Definition != null ? unit.Definition.UnitName : null;
        UpdateName(unitName);
    }

    public void Initialize(IReadOnlySquadModel squad)
    {
        Squad = squad;
        Unit = null;
        var unitName = squad?.UnitDefinition != null ? squad.UnitDefinition.UnitName : null;
        UpdateName(unitName);
    }

    private void UpdateName(string baseName)
    {
        if (string.IsNullOrEmpty(baseName))
            return;

        gameObject.name = $"{baseName}_BattleUnit";
    }
}
