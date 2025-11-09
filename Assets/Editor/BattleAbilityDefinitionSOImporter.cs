using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class BattleAbilityDefinitionSOImporter : IEntitiesSheetImporter
{
    private static readonly string[] RequiredColumns =
    {
        "ID",
        "AbilityName",
        "Description",
        "Icon",
        "Cooldown",
        "AbilityType",
        "AbilityTargetType",
        "Effects",
    };

    public string SheetName => "BattleAbilityDefinitionSO";

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

        var abilitySettings = settings.BattleAbility;
        if (abilitySettings == null)
        {
            GameLogger.Warn("[BattleAbilityDefinitionSOImporter] Battle ability settings are not configured.");
            return;
        }

        var folderAsset = abilitySettings.Folder;
        if (folderAsset == null)
        {
            GameLogger.Warn("[BattleAbilityDefinitionSOImporter] Target folder is not assigned.");
            return;
        }

        var folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] '{folderPath}' is not a valid folder.");
            return;
        }

        var effectsFolderAsset = abilitySettings.EffectsFolder;
        if (effectsFolderAsset == null)
        {
            GameLogger.Warn("[BattleAbilityDefinitionSOImporter] Effects folder is not assigned.");
            return;
        }

        var effectsFolderPath = AssetDatabase.GetAssetPath(effectsFolderAsset);
        if (string.IsNullOrEmpty(effectsFolderPath) || !AssetDatabase.IsValidFolder(effectsFolderPath))
        {
            GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] '{effectsFolderPath}' is not a valid effects folder.");
            return;
        }

        var spriteLookup = BuildSpriteLookup(abilitySettings.Sprites);

        var effects = LoadEffects(effectsFolderPath);
        if (effects.Count == 0)
        {
            GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] No BattleEffectDefinitionSO assets were found in '{effectsFolderPath}'.");
        }

        foreach (var column in RequiredColumns)
        {
            if (!sheet.Headers.Any(header => string.Equals(header, column, StringComparison.OrdinalIgnoreCase)))
            {
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Sheet '{sheet.Name}' is missing required column '{column}'.");
                return;
            }
        }

        foreach (var row in sheet.Rows)
        {
            var id = row.GetValueOrDefault("ID");
            if (string.IsNullOrWhiteSpace(id))
            {
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber} skipped: ID is empty.");
                continue;
            }

            var assetFileName = SanitizeFileName(id);
            if (string.IsNullOrEmpty(assetFileName))
            {
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber} skipped: ID '{id}' is not a valid file name.");
                continue;
            }

            var assetPath = Path.Combine(folderPath, assetFileName + ".asset").Replace('\\', '/');
            var ability = AssetDatabase.LoadAssetAtPath<BattleAbilityDefinitionSO>(assetPath);
            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<BattleAbilityDefinitionSO>();
                AssetDatabase.CreateAsset(ability, assetPath);
            }

            ability.Id = id.Trim();
            ability.AbilityName = row.GetValueOrDefault("AbilityName");
            ability.Description = row.GetValueOrDefault("Description");
            ability.Cooldown = ParseInt(row.GetValueOrDefault("Cooldown"), row.RowNumber, "Cooldown");
            ability.IsReady = ability.Cooldown <= 0;

            var iconValue = row.GetValueOrDefault("Icon");
            if (TryResolveSprite(iconValue, spriteLookup, row.RowNumber, out var sprite))
            {
                ability.Icon = sprite;
            }
            else
            {
                ability.Icon = null;
            }

            var abilityTypeValue = row.GetValueOrDefault("AbilityType");
            if (Enum.TryParse(abilityTypeValue, true, out BattleAbilityType abilityType))
            {
                ability.AbilityType = abilityType;
            }
            else if (!string.IsNullOrWhiteSpace(abilityTypeValue))
            {
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber}: AbilityType '{abilityTypeValue}' is invalid. Defaulting to Active.");
                ability.AbilityType = BattleAbilityType.Active;
            }
            else
            {
                ability.AbilityType = BattleAbilityType.Active;
            }

            var abilityTargetTypeValue = row.GetValueOrDefault("AbilityTargetType");
            if (Enum.TryParse(abilityTargetTypeValue, true, out BattleAbilityTargetType targetType))
            {
                ability.AbilityTargetType = targetType;
            }
            else if (!string.IsNullOrWhiteSpace(abilityTargetTypeValue))
            {
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber}: AbilityTargetType '{abilityTargetTypeValue}' is invalid. Defaulting to SingleEnemy.");
                ability.AbilityTargetType = BattleAbilityTargetType.SingleEnemy;
            }
            else
            {
                ability.AbilityTargetType = BattleAbilityTargetType.SingleEnemy;
            }

            var effectsValue = row.GetValueOrDefault("Effects");
            if (!string.IsNullOrWhiteSpace(effectsValue))
            {
                var effectNames = effectsValue
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(name => name.Trim())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();

                var resolvedEffects = new List<BattleEffectDefinitionSO>();
                foreach (var effectName in effectNames)
                {
                    if (effects.TryGetValue(effectName, out var effect))
                    {
                        resolvedEffects.Add(effect);
                    }
                    else
                    {
                        GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber}: Effect '{effectName}' not found in effects folder '{effectsFolderPath}'.");
                    }
                }

                ability.Effects = resolvedEffects.ToArray();
            }
            else
            {
                ability.Effects = Array.Empty<BattleEffectDefinitionSO>();
            }

            EditorUtility.SetDirty(ability);
        }

        AssetDatabase.SaveAssets();
    }

    private static Dictionary<string, BattleEffectDefinitionSO> LoadEffects(string folderPath)
    {
        var result = new Dictionary<string, BattleEffectDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        var guids = AssetDatabase.FindAssets("t:BattleEffectDefinitionSO", new[] { folderPath });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var effect = AssetDatabase.LoadAssetAtPath<BattleEffectDefinitionSO>(path);
            if (effect == null)
            {
                continue;
            }

            if (result.ContainsKey(effect.name))
            {
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Duplicate effect '{effect.name}' found in '{folderPath}'. Using the first occurrence.");
                continue;
            }

            result[effect.name] = effect;
        }

        return result;
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

        GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Row {rowNumber}: Icon '{value}' not found in configured sprites.");
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
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Sprites array contains an unassigned entry at index {i}.");
                continue;
            }

            if (result.ContainsKey(sprite.name))
            {
                GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Duplicate sprite name '{sprite.name}' found. Using the first occurrence.");
                continue;
            }

            result[sprite.name] = sprite;
        }

        return result;
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

        GameLogger.Warn($"[BattleAbilityDefinitionSOImporter] Row {rowNumber}: Unable to parse '{columnName}' value '{value}'. Using 0.");
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
