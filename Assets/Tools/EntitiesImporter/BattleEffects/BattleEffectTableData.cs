using System;
using System.Collections.Generic;
using System.Globalization;

public class BattleEffectTableData : IEntityTableData
{
    private readonly IReadOnlyDictionary<string, string> _fields;

    public BattleEffectTableData(
        string sheetName,
        IReadOnlyDictionary<string, string> fields,
        IReadOnlyList<BattleStatModifier> statsModifiers = null)
    {
        SheetName = sheetName;
        _fields = fields ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        StatsModifiers = statsModifiers ?? Array.Empty<BattleStatModifier>();
    }

    public string SheetName { get; }

    public IReadOnlyDictionary<string, string> Fields => _fields;

    public IReadOnlyList<BattleStatModifier> StatsModifiers { get; }

    public string Id => GetFieldValue("ID");

    public string Name => GetFieldValue("Name");

    public string Description => GetFieldValue("Description");

    public string IconKey => GetFieldValue("Icon");

    public string Trigger => GetFieldValue("Trigger");

    public bool TryGetValue(string columnName, out string value)
    {
        if (_fields == null)
        {
            value = null;
            return false;
        }

        return _fields.TryGetValue(columnName, out value);
    }

    public bool TryGetInt(string columnName, out int value)
    {
        if (TryGetValue(columnName, out var text) &&
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetFloat(string columnName, out float value)
    {
        if (TryGetValue(columnName, out var text) &&
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0f;
        return false;
    }

    private string GetFieldValue(string columnName)
    {
        return TryGetValue(columnName, out var value) ? value : string.Empty;
    }
}
