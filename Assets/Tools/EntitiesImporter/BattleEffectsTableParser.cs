using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

public class BattleEffectsTableParser : BaseEntityTableParser
{
    private static readonly string[] KnownSheetNames =
    {
        nameof(DamageBattleEffect),
        nameof(StatModifierBattleEffect)
    };

    public IReadOnlyList<string> SheetNames => KnownSheetNames;

    public override IEnumerable<IEntityTableData> Parse(string tableContent, string delimiter)
    {
        throw new NotSupportedException("Use LoadAllSheetsAsync to parse battle effects tables.");
    }

    public async Task<IReadOnlyList<BattleEffectsSheetResult>> LoadAllSheetsAsync(
        EntityTableLoader loader,
        string baseUrl,
        string delimiter)
    {
        if (loader == null)
        {
            throw new ArgumentNullException(nameof(loader));
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Array.Empty<BattleEffectsSheetResult>();
        }

        var results = new List<BattleEffectsSheetResult>();

        foreach (var sheetName in KnownSheetNames)
        {
            var sheetUrl = BuildSheetUrl(baseUrl, sheetName);
            var content = await loader.LoadTableAsync(sheetUrl).ConfigureAwait(false);

            var entries = ParseSheet(sheetName, content, delimiter, out var rawRowCount);
            results.Add(new BattleEffectsSheetResult(sheetName, entries, rawRowCount));
        }

        return results;
    }

    private static IReadOnlyList<BattleEffectTableData> ParseSheet(
        string sheetName,
        string csvContent,
        string delimiter,
        out int rawRowCount)
    {
        var records = ParseRecords(csvContent, delimiter);
        rawRowCount = records.Count;

        if (records.Count == 0)
        {
            return Array.Empty<BattleEffectTableData>();
        }

        if (string.Equals(sheetName, nameof(StatModifierBattleEffect), StringComparison.OrdinalIgnoreCase))
        {
            return ParseStatModifierSheet(records, sheetName);
        }

        return ParseDamageSheet(records, sheetName);
    }

    private static List<Dictionary<string, string>> ParseRecords(string csvContent, string delimiter)
    {
        var records = new List<Dictionary<string, string>>();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return records;
        }

        var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var delimiterChar = ResolveDelimiter(delimiter);

        string[] headers = null;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.TrimEnd('\r');
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

            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length && i < columns.Length; i++)
            {
                var header = headers[i]?.Trim();
                if (string.IsNullOrEmpty(header))
                {
                    continue;
                }

                var value = columns[i]?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                record[header] = value;
            }

            if (record.Count > 0)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static IReadOnlyList<BattleEffectTableData> ParseDamageSheet(
        List<Dictionary<string, string>> records,
        string sheetName)
    {
        var results = new List<BattleEffectTableData>();

        foreach (var record in records)
        {
            if (!record.TryGetValue("ID", out var id) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var fields = new Dictionary<string, string>(record, StringComparer.OrdinalIgnoreCase);
            results.Add(new BattleEffectTableData(sheetName, fields));
        }

        return results;
    }

    private static IReadOnlyList<BattleEffectTableData> ParseStatModifierSheet(
        List<Dictionary<string, string>> records,
        string sheetName)
    {
        var grouped = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (!record.TryGetValue("ID", out var id) || string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (!grouped.TryGetValue(id, out var list))
            {
                list = new List<Dictionary<string, string>>();
                grouped.Add(id, list);
            }

            list.Add(record);
        }

        var results = new List<BattleEffectTableData>();

        foreach (var kvp in grouped)
        {
            var mergedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = kvp.Key
            };

            var modifiers = new List<BattleStatModifier>();

            foreach (var record in kvp.Value)
            {
                foreach (var pair in record)
                {
                    if (IsStatColumn(pair.Key))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(pair.Value))
                    {
                        continue;
                    }

                    mergedFields[pair.Key] = pair.Value;
                }

                if (!record.TryGetValue("Stat", out var statName) || string.IsNullOrWhiteSpace(statName))
                {
                    continue;
                }

                if (!record.TryGetValue("Value", out var valueText))
                {
                    continue;
                }

                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                if (!Enum.TryParse(statName, true, out BattleSquadStat stat))
                {
                    continue;
                }

                modifiers.Add(new BattleStatModifier(stat, value));
            }

            results.Add(new BattleEffectTableData(sheetName, mergedFields, modifiers));
        }

        return results;
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

            if (current == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    builder.Append('\"');
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
                values.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(current);
        }

        values.Add(builder.ToString());

        for (int i = 0; i < values.Count; i++)
        {
            values[i] = values[i].Trim().Trim('\"');
        }

        return values.ToArray();
    }

    private static bool IsStatColumn(string columnName)
    {
        return string.Equals(columnName, "Stat", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(columnName, "Value", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSheetUrl(string baseUrl, string sheetName)
    {
        if (baseUrl.IndexOf("{sheet}", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return baseUrl.Replace("{sheet}", sheetName);
        }

        var sheetParamIndex = baseUrl.IndexOf("sheet=", StringComparison.OrdinalIgnoreCase);
        if (sheetParamIndex >= 0)
        {
            var startIndex = sheetParamIndex + "sheet=".Length;
            var endIndex = baseUrl.IndexOf('&', startIndex);
            if (endIndex < 0)
            {
                endIndex = baseUrl.Length;
            }

            return string.Concat(
                baseUrl.AsSpan(0, startIndex),
                Uri.EscapeDataString(sheetName),
                baseUrl.AsSpan(endIndex));
        }

        var separator = baseUrl.Contains("?") ? "&" : "?";
        return $"{baseUrl}{separator}sheet={Uri.EscapeDataString(sheetName)}";
    }

    public sealed class BattleEffectsSheetResult
    {
        public BattleEffectsSheetResult(
            string sheetName,
            IReadOnlyList<BattleEffectTableData> entries,
            int rawRowCount)
        {
            SheetName = sheetName;
            Entries = entries ?? Array.Empty<BattleEffectTableData>();
            RawRowCount = rawRowCount;
        }

        public string SheetName { get; }
        public IReadOnlyList<BattleEffectTableData> Entries { get; }
        public int RawRowCount { get; }
    }
}
