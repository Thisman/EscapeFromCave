using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class BattleEffectDamageSOImporter : IEntitiesSheetImporter
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

    public string SheetName => "BattleEffectDamageSO";

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
            GameLogger.Warn("[DamageBattleEffectImporter] Battle effect settings are not configured.");
            return;
        }

        var folderAsset = battleEffectSettings.Folder;
        if (folderAsset == null)
        {
            GameLogger.Warn("[DamageBattleEffectImporter] Target folder is not assigned.");
            return;
        }

        var folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            GameLogger.Warn($"[DamageBattleEffectImporter] '{folderPath}' is not a valid folder.");
            return;
        }

        var spriteLookup = BuildSpriteLookup(battleEffectSettings.Sprites);

        foreach (var column in RequiredColumns)
        {
            if (!sheet.Headers.Any(header => string.Equals(header, column, StringComparison.OrdinalIgnoreCase)))
            {
                GameLogger.Warn($"[DamageBattleEffectImporter] Sheet '{sheet.Name}' is missing required column '{column}'.");
                return;
            }
        }

        foreach (var row in sheet.Rows)
        {
            var id = row.GetValueOrDefault("ID");
            if (string.IsNullOrWhiteSpace(id))
            {
                GameLogger.Warn($"[DamageBattleEffectImporter] Row {row.RowNumber} skipped: ID is empty.");
                continue;
            }

            var assetFileName = SanitizeFileName(id);
            if (string.IsNullOrEmpty(assetFileName))
            {
                GameLogger.Warn($"[DamageBattleEffectImporter] Row {row.RowNumber} skipped: ID '{id}' is not a valid file name.");
                continue;
            }

            var assetPath = Path.Combine(folderPath, assetFileName + ".asset").Replace('\\', '/');
            var effect = AssetDatabase.LoadAssetAtPath<BattleEffectDamageSO>(assetPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<BattleEffectDamageSO>();
                AssetDatabase.CreateAsset(effect, assetPath);
            }

            effect.Name = row.GetValueOrDefault("Name");
            effect.Description = row.GetValueOrDefault("Description");

            var iconValue = row.GetValueOrDefault("Icon");
            if (TryResolveSprite(iconValue, spriteLookup, row.RowNumber, out var sprite))
            {
                effect.Icon = sprite;
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
                GameLogger.Warn($"[DamageBattleEffectImporter] Row {row.RowNumber}: Trigger '{triggerValue}' is invalid. Defaulting to OnAttach.");
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

        GameLogger.Warn($"[DamageBattleEffectImporter] Row {rowNumber}: Unable to parse '{columnName}' value '{value}'. Using 0.");
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

        GameLogger.Warn($"[DamageBattleEffectImporter] Row {rowNumber}: Icon '{value}' not found in configured sprites.");
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
                GameLogger.Warn($"[DamageBattleEffectImporter] Sprites array contains an unassigned entry at index {i}.");
                continue;
            }

            if (result.ContainsKey(sprite.name))
            {
                GameLogger.Warn($"[DamageBattleEffectImporter] Duplicate sprite name '{sprite.name}' found. Using the first occurrence.");
                continue;
            }

            result[sprite.name] = sprite;
        }

        return result;
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
