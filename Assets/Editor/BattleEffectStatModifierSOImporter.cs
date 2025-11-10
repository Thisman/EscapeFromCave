using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class BattleEffectStatModifierSOImporter : IEntitiesSheetImporter
{
    private static readonly string[] RequiredColumns =
    {
        "ID",
        "Name",
        "Description",
        "Icon",
        "Trigger",
        "MaxTick",
        "Stat",
        "Value"
    };

    public string SheetName => "BattleEffectStatsModifierSO";

    public void Import(EntitiesSheet sheet, EntitiesImporterSettings settings)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var battleEffectSettings = settings.BattleEffect;
        if (battleEffectSettings == null)
        {
            GameLogger.Warn("[StatModifierBattleEffectImporter] Battle effect settings are not configured.");
            return;
        }

        var folderAsset = battleEffectSettings.Folder;
        if (folderAsset == null)
        {
            GameLogger.Warn("[StatModifierBattleEffectImporter] Target folder is not assigned.");
            return;
        }

        var folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            GameLogger.Warn($"[StatModifierBattleEffectImporter] '{folderPath}' is not a valid folder.");
            return;
        }

        var spriteLookup = BuildSpriteLookup(battleEffectSettings.Sprites);

        foreach (var column in RequiredColumns)
        {
            if (!sheet.Headers.Any(header => string.Equals(header, column, StringComparison.OrdinalIgnoreCase)))
            {
                GameLogger.Warn($"[StatModifierBattleEffectImporter] Sheet '{sheet.Name}' is missing required column '{column}'.");
                return;
            }
        }

        var groups = sheet.Rows
            .Select(row => new
            {
                Row = row,
                Id = row.GetValueOrDefault("ID").Trim()
            })
            .Where(data => !string.IsNullOrWhiteSpace(data.Id))
            .GroupBy(data => data.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var firstRow = group.First().Row;
            var assetFileName = SanitizeFileName(group.Key);
            if (string.IsNullOrEmpty(assetFileName))
            {
                GameLogger.Warn($"[StatModifierBattleEffectImporter] Rows with ID '{group.Key}' skipped: not a valid file name.");
                continue;
            }

            var assetPath = Path.Combine(folderPath, assetFileName + ".asset").Replace('\\', '/');
            var effect = AssetDatabase.LoadAssetAtPath<BattleEffectStatsModifierSO>(assetPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<BattleEffectStatsModifierSO>();
                AssetDatabase.CreateAsset(effect, assetPath);
            }

            effect.Name = firstRow.GetValueOrDefault("Name");
            effect.Description = firstRow.GetValueOrDefault("Description");

            var iconValue = firstRow.GetValueOrDefault("Icon");
            if (TryResolveSprite(iconValue, spriteLookup, firstRow.RowNumber, out var sprite))
            {
                effect.Icon = sprite;
            }
            else
            {
                effect.Icon = null;
            }

            var triggerValue = firstRow.GetValueOrDefault("Trigger");
            if (Enum.TryParse(triggerValue, true, out BattleEffectTrigger trigger))
            {
                effect.Trigger = trigger;
            }
            else if (!string.IsNullOrWhiteSpace(triggerValue))
            {
                GameLogger.Warn($"[StatModifierBattleEffectImporter] Row {firstRow.RowNumber}: Trigger '{triggerValue}' is invalid. Defaulting to OnAttach.");
                effect.Trigger = BattleEffectTrigger.OnAttach;
            }
            else
            {
                effect.Trigger = BattleEffectTrigger.OnAttach;
            }

            effect.MaxTick = ParseInt(firstRow.GetValueOrDefault("MaxTick"), firstRow.RowNumber, "MaxTick");

            var modifiers = new List<BattleStatModifier>();
            foreach (var data in group)
            {
                var row = data.Row;

                var statName = row.GetValueOrDefault("Stat");
                if (string.IsNullOrWhiteSpace(statName))
                {
                    GameLogger.Warn($"[StatModifierBattleEffectImporter] Row {row.RowNumber}: Stat is empty. Skipping modifier.");
                    continue;
                }

                if (!Enum.TryParse(statName, true, out BattleSquadStat stat))
                {
                    GameLogger.Warn($"[StatModifierBattleEffectImporter] Row {row.RowNumber}: Stat '{statName}' is invalid. Skipping modifier.");
                    continue;
                }

                var valueText = row.GetValueOrDefault("Value");
                if (!TryParseFloat(valueText, out var value))
                {
                    GameLogger.Warn($"[StatModifierBattleEffectImporter] Row {row.RowNumber}: Value '{valueText}' is invalid. Skipping modifier.");
                    continue;
                }

                modifiers.Add(new BattleStatModifier(stat, value));
            }

            effect.StatsModifier = modifiers.ToArray();
            EditorUtility.SetDirty(effect);
        }

        AssetDatabase.SaveAssets();
    }

    private static int ParseInt(string value, int rowNumber, string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatParsed))
        {
            return Mathf.RoundToInt(floatParsed);
        }

        GameLogger.Warn($"[StatModifierBattleEffectImporter] Row {rowNumber}: Unable to parse '{columnName}' value '{value}'. Using 0.");
        return 0;
    }

    private static bool TryResolveSprite(string value, Dictionary<string, Sprite> sprites, int rowNumber, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (sprites.TryGetValue(value.Trim(), out sprite))
        {
            return true;
        }

        GameLogger.Warn($"[StatModifierBattleEffectImporter] Row {rowNumber}: Icon '{value}' not found in configured sprites.");
        return false;
    }

    private static Dictionary<string, Sprite> BuildSpriteLookup(IReadOnlyList<Sprite> sprites)
    {
        var result = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        if (sprites == null)
        {
            return result;
        }

        for (var i = 0; i < sprites.Count; i++)
        {
            var sprite = sprites[i];
            if (sprite == null)
            {
                GameLogger.Warn($"[StatModifierBattleEffectImporter] Sprites array contains an unassigned entry at index {i}.");
                continue;
            }

            if (result.ContainsKey(sprite.name))
            {
                GameLogger.Warn($"[StatModifierBattleEffectImporter] Duplicate sprite name '{sprite.name}' found. Using the first occurrence.");
                continue;
            }

            result[sprite.name] = sprite;
        }

        return result;
    }

    private static bool TryParseFloat(string value, out float result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0f;
            return true;
        }

        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        result = 0f;
        return false;
    }

    private static string SanitizeFileName(string name)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var result = new char[name.Length];
        var index = 0;

        foreach (var character in name)
        {
            if (invalidCharacters.Contains(character))
            {
                continue;
            }

            result[index++] = character;
        }

        return new string(result, 0, index).Trim();
    }
}
