using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class StatModifierBattleEffectImporter : IEntitiesSheetImporter
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

    public string SheetName => "StatModifierBattleEffect";

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
            Debug.LogWarning("[StatModifierBattleEffectImporter] Battle effect settings are not configured.");
            return;
        }

        var folderAsset = battleEffectSettings.Folder;
        if (folderAsset == null)
        {
            Debug.LogWarning("[StatModifierBattleEffectImporter] Target folder is not assigned.");
            return;
        }

        var folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[StatModifierBattleEffectImporter] '{folderPath}' is not a valid folder.");
            return;
        }

        var sprites = battleEffectSettings.Sprites?.ToArray() ?? Array.Empty<Sprite>();

        foreach (var column in RequiredColumns)
        {
            if (!sheet.Headers.Any(header => string.Equals(header, column, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning($"[StatModifierBattleEffectImporter] Sheet '{sheet.Name}' is missing required column '{column}'.");
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
                Debug.LogWarning($"[StatModifierBattleEffectImporter] Rows with ID '{group.Key}' skipped: not a valid file name.");
                continue;
            }

            var assetPath = Path.Combine(folderPath, assetFileName + ".asset").Replace('\\', '/');
            var effect = AssetDatabase.LoadAssetAtPath<StatModifierBattleEffect>(assetPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<StatModifierBattleEffect>();
                AssetDatabase.CreateAsset(effect, assetPath);
            }

            effect.Name = firstRow.GetValueOrDefault("Name");
            effect.Description = firstRow.GetValueOrDefault("Description");

            var iconValue = firstRow.GetValueOrDefault("Icon");
            if (TryGetSpriteIndex(iconValue, firstRow.RowNumber, out var iconIndex))
            {
                if (iconIndex >= 0 && iconIndex < sprites.Length)
                {
                    var sprite = sprites[iconIndex];
                    if (sprite != null)
                    {
                        effect.Icon = sprite;
                    }
                    else
                    {
                        Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {firstRow.RowNumber}: Icon at index {iconIndex} is not assigned in sprites array.");
                        effect.Icon = null;
                    }
                }
                else
                {
                    Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {firstRow.RowNumber}: Icon index {iconIndex} is out of range for sprites array (size {sprites.Length}).");
                    effect.Icon = null;
                }
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
                Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {firstRow.RowNumber}: Trigger '{triggerValue}' is invalid. Defaulting to OnAttach.");
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
                    Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {row.RowNumber}: Stat is empty. Skipping modifier.");
                    continue;
                }

                if (!Enum.TryParse(statName, true, out BattleSquadStat stat))
                {
                    Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {row.RowNumber}: Stat '{statName}' is invalid. Skipping modifier.");
                    continue;
                }

                var valueText = row.GetValueOrDefault("Value");
                if (!TryParseFloat(valueText, out var value))
                {
                    Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {row.RowNumber}: Value '{valueText}' is invalid. Skipping modifier.");
                    continue;
                }

                modifiers.Add(new BattleStatModifier(stat, value));
            }

            effect._statModifiers = modifiers.ToArray();
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

        Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {rowNumber}: Unable to parse '{columnName}' value '{value}'. Using 0.");
        return 0;
    }

    private static bool TryGetSpriteIndex(string value, int rowNumber, out int index)
    {
        index = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!TryParseInteger(value, out var parsed))
        {
            Debug.LogWarning($"[StatModifierBattleEffectImporter] Row {rowNumber}: Icon '{value}' is not a valid integer. Icon will be cleared.");
            return false;
        }

        index = parsed;
        return true;
    }

    private static bool TryParseInteger(string value, out int result)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatParsed))
        {
            result = Mathf.RoundToInt(floatParsed);
            return true;
        }

        result = 0;
        return false;
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
