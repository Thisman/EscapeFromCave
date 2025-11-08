using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class DamageBattleEffectImporter : IEntitiesSheetImporter
{
    private static readonly string[] RequiredColumns =
    {
        "ID",
        "Name",
        "Description",
        "Icon",
        "Trigger",
        "MaxTick",
        "Damage"
    };

    public string SheetName => "DamageBattleEffect";

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
            Debug.LogWarning("[DamageBattleEffectImporter] Battle effect settings are not configured.");
            return;
        }

        var folderAsset = battleEffectSettings.Folder;
        if (folderAsset == null)
        {
            Debug.LogWarning("[DamageBattleEffectImporter] Target folder is not assigned.");
            return;
        }

        var folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[DamageBattleEffectImporter] '{folderPath}' is not a valid folder.");
            return;
        }

        var sprites = (battleEffectSettings.Sprites ?? Array.Empty<Sprite>())
            .Where(sprite => sprite != null)
            .GroupBy(sprite => sprite.name)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var column in RequiredColumns)
        {
            if (!sheet.Headers.Any(header => string.Equals(header, column, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning($"[DamageBattleEffectImporter] Sheet '{sheet.Name}' is missing required column '{column}'.");
                return;
            }
        }

        foreach (var row in sheet.Rows)
        {
            var id = row.GetValueOrDefault("ID");
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[DamageBattleEffectImporter] Row {row.RowNumber} skipped: ID is empty.");
                continue;
            }

            var assetFileName = SanitizeFileName(id);
            if (string.IsNullOrEmpty(assetFileName))
            {
                Debug.LogWarning($"[DamageBattleEffectImporter] Row {row.RowNumber} skipped: ID '{id}' is not a valid file name.");
                continue;
            }

            var assetPath = Path.Combine(folderPath, assetFileName + ".asset").Replace('\\', '/');
            var effect = AssetDatabase.LoadAssetAtPath<DamageBattleEffect>(assetPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<DamageBattleEffect>();
                AssetDatabase.CreateAsset(effect, assetPath);
            }

            effect.Name = row.GetValueOrDefault("Name");
            effect.Description = row.GetValueOrDefault("Description");

            var iconName = row.GetValueOrDefault("Icon");
            if (!string.IsNullOrWhiteSpace(iconName) && sprites.TryGetValue(iconName, out var sprite))
            {
                effect.Icon = sprite;
            }
            else if (!string.IsNullOrWhiteSpace(iconName))
            {
                Debug.LogWarning($"[DamageBattleEffectImporter] Row {row.RowNumber}: Icon '{iconName}' not found in configured sprites.");
                effect.Icon = null;
            }
            else
            {
                effect.Icon = null;
            }

            var triggerValue = row.GetValueOrDefault("Trigger");
            if (Enum.TryParse(triggerValue, true, out BattleEffectTrigger trigger))
            {
                effect.Trigger = trigger;
            }
            else if (!string.IsNullOrWhiteSpace(triggerValue))
            {
                Debug.LogWarning($"[DamageBattleEffectImporter] Row {row.RowNumber}: Trigger '{triggerValue}' is invalid. Defaulting to OnAttach.");
                effect.Trigger = BattleEffectTrigger.OnAttach;
            }
            else
            {
                effect.Trigger = BattleEffectTrigger.OnAttach;
            }

            effect.MaxTick = ParseInt(row.GetValueOrDefault("MaxTick"), row.RowNumber, "MaxTick");
            effect.Damage = ParseInt(row.GetValueOrDefault("Damage"), row.RowNumber, "Damage");

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

        Debug.LogWarning($"[DamageBattleEffectImporter] Row {rowNumber}: Unable to parse '{columnName}' value '{value}'. Using 0.");
        return 0;
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
