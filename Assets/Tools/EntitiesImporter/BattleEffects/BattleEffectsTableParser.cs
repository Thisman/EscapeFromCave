using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public class BattleEffectsTableParser : BaseEntityTableParser
{
    private static readonly string[] KnownSheetNames =
    {
        nameof(DamageBattleEffect),
        nameof(StatModifierBattleEffect)
    };

    public override IEnumerable<IEntityTableData> Parse(string tableContent, string delimiter)
    {
        if (string.IsNullOrWhiteSpace(tableContent))
        {
            yield break;
        }

        var rows = ParseRows(tableContent, delimiter).ToList();
        if (rows.Count == 0)
        {
            yield break;
        }

        var groupedRows = new Dictionary<string, List<SheetRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!row.Fields.TryGetValue("ID", out var id) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var key = $"{row.SheetName}::{id}";
            if (!groupedRows.TryGetValue(key, out var list))
            {
                list = new List<SheetRow>();
                groupedRows.Add(key, list);
            }

            list.Add(row);
        }

        foreach (var group in groupedRows.Values)
        {
            if (group.Count == 0)
            {
                continue;
            }

            var firstRow = group[0];
            var fields = BuildFields(group);
            var modifiers = BuildStatModifiers(firstRow.SheetName, group);

            yield return new BattleEffectTableData(firstRow.SheetName, fields, modifiers);
        }
    }

    private static IEnumerable<SheetRow> ParseRows(string tableContent, string delimiter)
    {
        var rows = new List<SheetRow>();

        var lines = tableContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var delimiterChar = ResolveDelimiter(delimiter);

        string currentSheet = null;
        string[] headers = null;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.TrimEnd('\r');
            if (IsSheetName(line))
            {
                currentSheet = line.Trim();
                headers = null;
                continue;
            }

            if (currentSheet == null)
            {
                continue;
            }

            var columns = SplitLine(line, delimiterChar);
            if (columns.Length == 0)
            {
                continue;
            }

            if (headers == null)
            {
                headers = columns;
                continue;
            }

            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length && i < columns.Length; i++)
            {
                var header = headers[i];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                fields[header.Trim()] = columns[i];
            }

            if (fields.Count > 0)
            {
                rows.Add(new SheetRow(currentSheet, fields));
            }
        }

        return rows;
    }

    private static Dictionary<string, string> BuildFields(IEnumerable<SheetRow> rows)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            foreach (var kvp in row.Fields)
            {
                if (string.Equals(kvp.Key, "Stat", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, "Value", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(kvp.Value))
                {
                    continue;
                }

                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private static IReadOnlyList<BattleStatModifier> BuildStatModifiers(string sheetName, IEnumerable<SheetRow> rows)
    {
        if (!string.Equals(sheetName, nameof(StatModifierBattleEffect), StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<BattleStatModifier>();
        }

        var modifiers = new List<BattleStatModifier>();
        foreach (var row in rows)
        {
            if (!row.Fields.TryGetValue("Stat", out var statName) || string.IsNullOrWhiteSpace(statName))
            {
                continue;
            }

            if (!row.Fields.TryGetValue("Value", out var valueText) ||
                !float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            if (!Enum.TryParse(statName, true, out BattleSquadStat stat))
            {
                continue;
            }

            modifiers.Add(new BattleStatModifier(stat, value));
        }

        return modifiers;
    }

    private static bool IsSheetName(string line)
    {
        var trimmed = line.Trim();
        return KnownSheetNames.Any(sheet => string.Equals(sheet, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static char ResolveDelimiter(string delimiter)
    {
        return string.IsNullOrEmpty(delimiter) ? ',' : delimiter[0];
    }

    private static string[] SplitLine(string line, char delimiter)
    {
        var values = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var current = line[i];

            if (current == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (current == delimiter && !inQuotes)
            {
                values.Add(builder.ToString().Trim());
                builder.Clear();
                continue;
            }

            builder.Append(current);
        }

        values.Add(builder.ToString().Trim());

        for (int i = 0; i < values.Count; i++)
        {
            values[i] = values[i].Trim('"');
        }

        return values.ToArray();
    }

    private readonly struct SheetRow
    {
        public SheetRow(string sheetName, Dictionary<string, string> fields)
        {
            SheetName = sheetName;
            Fields = fields;
        }

        public string SheetName { get; }

        public Dictionary<string, string> Fields { get; }
    }
}
