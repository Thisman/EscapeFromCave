#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DamageBattleEffectsCreator
{
    private const string IdColumn = "ID";
    private const string NameColumn = "Name";
    private const string DescriptionColumn = "Description";
    private const string IconColumn = "Icon";
    private const string TriggerColumn = "Trigger";
    private const string MaxTickColumn = "MaxTick";
    private const string DamageColumn = "Damage";

    public static void CreateFromFile(string filePath, DownloadSettings settings)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is empty", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("Table file not found", filePath);
        var text = File.ReadAllText(filePath);
        CreateFromText(text, settings, Path.GetFileName(filePath));
    }

    public static void CreateFromText(string tableText, DownloadSettings settings, string sourceName = null)
    {
        if (string.IsNullOrWhiteSpace(tableText)) throw new ArgumentException("Table text is empty", nameof(tableText));
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        var delimiter = ResolveDelimiter(settings, sourceName);
        var rows = ParseTable(tableText, delimiter);

        var battleEffectsFolder = ResolveBattleEffectsRoot(settings);
        EnsureFolderExists(battleEffectsFolder);

        foreach (var row in rows)
        {
            CreateOrUpdateEffectAsset(row, battleEffectsFolder, settings);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateOrUpdateEffectAsset(Dictionary<string, string> row, string rootFolder, DownloadSettings settings)
    {
        if (!row.TryGetValue(IdColumn, out var id) || string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("Row skipped: missing ID column value.");
            return;
        }

        var assetPath = Path.Combine(rootFolder, id + ".asset").Replace('\\', '/');
        DamageBattleEffect asset = AssetDatabase.LoadAssetAtPath<DamageBattleEffect>(assetPath);
        var isNew = asset == null;
        if (isNew)
        {
            asset = ScriptableObject.CreateInstance<DamageBattleEffect>();
            asset.name = id;
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.Name = row.TryGetValue(NameColumn, out var name) ? name : string.Empty;
        asset.Description = row.TryGetValue(DescriptionColumn, out var description) ? description : string.Empty;
        asset.Icon = ResolveIcon(row, settings);
        asset.Trigger = ParseEnum(row, TriggerColumn, BattleEffectTrigger.OnAttach);
        asset.MaxTick = ParseInt(row, MaxTickColumn, 0);
        asset.Damage = ParseInt(row, DamageColumn, 0);

        EditorUtility.SetDirty(asset);

        if (isNew)
        {
            Debug.Log($"Created DamageBattleEffect asset: {assetPath}");
        }
        else
        {
            Debug.Log($"Updated DamageBattleEffect asset: {assetPath}");
        }
    }

    private static Sprite ResolveIcon(Dictionary<string, string> row, DownloadSettings settings)
    {
        if (!row.TryGetValue(IconColumn, out var iconName) || string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        iconName = iconName.Trim();
        if (iconName.Length == 0)
        {
            return null;
        }

        var sprites = settings.sprites;
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("DamageBattleEffectsCreator: список спрайтов пуст в DownloadSettings.");
            return null;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            var sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            if (string.Equals(sprite.name, iconName, StringComparison.OrdinalIgnoreCase))
            {
                return sprite;
            }
        }

        Debug.LogWarning($"Sprite '{iconName}' not found in configured sprites list.");
        return null;
    }

    private static BattleEffectTrigger ParseEnum(Dictionary<string, string> row, string column, BattleEffectTrigger defaultValue)
    {
        if (row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            try
            {
                return (BattleEffectTrigger)Enum.Parse(typeof(BattleEffectTrigger), value, true);
            }
            catch (Exception)
            {
                Debug.LogWarning($"Cannot parse '{value}' as {nameof(BattleEffectTrigger)}. Using default {defaultValue}.");
            }
        }

        return defaultValue;
    }

    private static int ParseInt(Dictionary<string, string> row, string column, int defaultValue)
    {
        if (row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            Debug.LogWarning($"Cannot parse '{value}' as integer. Using default {defaultValue}.");
        }

        return defaultValue;
    }

    private static IEnumerable<Dictionary<string, string>> ParseTable(string tableText, char delimiter)
    {
        var result = new List<Dictionary<string, string>>();
        using var reader = new StringReader(tableText);
        string line;
        List<string> headers = null;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = SplitLine(line, delimiter);
            if (headers == null)
            {
                headers = values.Select(v => v.Trim()).ToList();
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                var header = headers[i];
                if (string.IsNullOrEmpty(header))
                {
                    continue;
                }

                row[header] = values[i].Trim();
            }

            if (row.Count > 0)
            {
                result.Add(row);
            }
        }

        ValidateHeaders(headers);

        return result;
    }

    private static void ValidateHeaders(List<string> headers)
    {
        if (headers == null)
        {
            throw new InvalidDataException("Table does not contain headers.");
        }

        var required = new[] { IdColumn, NameColumn, DescriptionColumn, IconColumn, TriggerColumn, MaxTickColumn, DamageColumn };
        foreach (var header in required)
        {
            if (!headers.Any(h => string.Equals(h, header, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException($"Table is missing required column '{header}'.");
            }
        }
    }

    private static List<string> SplitLine(string line, char delimiter)
    {
        var values = new List<string>();
        if (line == null)
        {
            return values;
        }

        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                values.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        values.Add(sb.ToString());
        return values;
    }

    private static char ResolveDelimiter(DownloadSettings settings, string sourceName)
    {
        return settings.delimiterMode switch
        {
            DelimiterMode.Comma => ',',
            DelimiterMode.Semicolon => ';',
            DelimiterMode.Tab => '\t',
            DelimiterMode.Custom => GetCustomDelimiter(settings),
            _ => AutoDetectDelimiter(sourceName, settings)
        };
    }

    private static char AutoDetectDelimiter(string sourceName, DownloadSettings settings)
    {
        if (!string.IsNullOrEmpty(sourceName))
        {
            var ext = Path.GetExtension(sourceName)?.ToLowerInvariant();
            if (ext == ".tsv")
            {
                return '\t';
            }

            if (ext == ".csv")
            {
                return ',';
            }
        }

        var custom = GetCustomDelimiter(settings);
        return custom != '\0' ? custom : ',';
    }

    private static char GetCustomDelimiter(DownloadSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.customDelimiter))
        {
            return settings.customDelimiter[0];
        }

        return '\0';
    }

    private static void EnsureFolderExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        parent = parent.Replace('\\', '/');
        EnsureFolderExists(parent);

        var folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(folderName) && !AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static string ResolveBattleEffectsRoot(DownloadSettings settings)
    {
        if (settings.battleEffectsRootFolder == null)
        {
            throw new InvalidOperationException("Battle effects root folder is not specified in DownloadSettings.");
        }

        var folderPath = AssetDatabase.GetAssetPath(settings.battleEffectsRootFolder);
        if (string.IsNullOrEmpty(folderPath))
        {
            throw new InvalidOperationException("Cannot determine path for the battle effects root folder asset.");
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            throw new InvalidOperationException($"Assigned battle effects root asset is not a folder: {folderPath}");
        }

        return folderPath;
    }
}
#endif
