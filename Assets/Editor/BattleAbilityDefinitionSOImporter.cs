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
            Debug.LogWarning("[BattleAbilityDefinitionSOImporter] Battle ability settings are not configured.");
            return;
        }

        var folderAsset = abilitySettings.Folder;
        if (folderAsset == null)
        {
            Debug.LogWarning("[BattleAbilityDefinitionSOImporter] Target folder is not assigned.");
            return;
        }

        var folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] '{folderPath}' is not a valid folder.");
            return;
        }

        var effectsFolderAsset = abilitySettings.EffectsFolder;
        if (effectsFolderAsset == null)
        {
            Debug.LogWarning("[BattleAbilityDefinitionSOImporter] Effects folder is not assigned.");
            return;
        }

        var effectsFolderPath = AssetDatabase.GetAssetPath(effectsFolderAsset);
        if (string.IsNullOrEmpty(effectsFolderPath) || !AssetDatabase.IsValidFolder(effectsFolderPath))
        {
            Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] '{effectsFolderPath}' is not a valid effects folder.");
            return;
        }

        var sprites = (abilitySettings.Sprites ?? Array.Empty<Sprite>())
            .Where(sprite => sprite != null)
            .GroupBy(sprite => sprite.name)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var effects = LoadEffects(effectsFolderPath);
        if (effects.Count == 0)
        {
            Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] No BattleEffectDefinitionSO assets were found in '{effectsFolderPath}'.");
        }

        foreach (var column in RequiredColumns)
        {
            if (!sheet.Headers.Any(header => string.Equals(header, column, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Sheet '{sheet.Name}' is missing required column '{column}'.");
                return;
            }
        }

        foreach (var row in sheet.Rows)
        {
            var id = row.GetValueOrDefault("ID");
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber} skipped: ID is empty.");
                continue;
            }

            var assetFileName = SanitizeFileName(id);
            if (string.IsNullOrEmpty(assetFileName))
            {
                Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber} skipped: ID '{id}' is not a valid file name.");
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
            if (TryGetSpriteKey(iconValue, row.RowNumber, out var iconKey))
            {
                if (sprites.TryGetValue(iconKey, out var sprite))
                {
                    ability.Icon = sprite;
                }
                else
                {
                    Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber}: Icon '{iconKey}' not found in configured sprites.");
                    ability.Icon = null;
                }
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
                Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber}: AbilityType '{abilityTypeValue}' is invalid. Defaulting to Active.");
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
                Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber}: AbilityTargetType '{abilityTargetTypeValue}' is invalid. Defaulting to SingleEnemy.");
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
                        Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {row.RowNumber}: Effect '{effectName}' not found in effects folder '{effectsFolderPath}'.");
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
                Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Duplicate effect '{effect.name}' found in '{folderPath}'. Using the first occurrence.");
                continue;
            }

            result[effect.name] = effect;
        }

        return result;
    }

    private static bool TryGetSpriteKey(string value, int rowNumber, out string key)
    {
        key = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!TryParseInteger(value, out var parsed))
        {
            Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {rowNumber}: Icon '{value}' is not a valid integer. Icon will be cleared.");
            return false;
        }

        key = parsed.ToString(CultureInfo.InvariantCulture);
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

        Debug.LogWarning($"[BattleAbilityDefinitionSOImporter] Row {rowNumber}: Unable to parse '{columnName}' value '{value}'. Using 0.");
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
