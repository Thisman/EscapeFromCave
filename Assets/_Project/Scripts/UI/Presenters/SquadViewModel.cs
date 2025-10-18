using UnityEngine;

public sealed class SquadViewModel
{
    public SquadModel Squad { get; }
    public string UnitId { get; }
    public string DisplayName { get; }
    public Sprite Icon { get; }
    public int Count { get; }

    private SquadViewModel(SquadModel squad, string unitId, string displayName, Sprite icon, int count)
    {
        Squad = squad;
        UnitId = unitId;
        DisplayName = displayName;
        Icon = icon;
        Count = count;
    }

    public static SquadViewModel FromSquad(SquadModel squad)
    {
        if (squad == null || squad.IsEmpty)
        {
            return new SquadViewModel(null, string.Empty, string.Empty, null, 0);
        }

        var definition = squad.UnitDefinition;
        string id = definition?.Id ?? string.Empty;
        string displayName = id;
        Sprite icon = null;

        if (definition is UnitDefinitionSO so)
        {
            displayName = string.IsNullOrWhiteSpace(so.UnitName) ? so.name : so.UnitName;
            icon = so.Icon;
        }

        return new SquadViewModel(squad, id, displayName, icon, squad.Count);
    }
}
