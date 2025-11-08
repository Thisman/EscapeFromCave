using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class UnitDefinitionSOImporter : IEntitiesSheetImporter
{
    private static readonly string[] RequiredColumns =
    {
        "ID",
        "Icon",
        "UnitName",
        "Kind",
        "AttackKind",
        "DamageType",
        "BaseHealth",
        "BasePhysicalDefense",
        "BaseMagicDefense",
        "BaseAbsoluteDefense",
        "MinDamage",
        "MaxDamage",
        "Speed",
        "BaseCritChance",
        "BaseCritMultiplier",
        "BaseMissChance",
        "Abilities",
    };

    public string SheetName => "UnitDefinitionSO";

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

        var unitSettings = settings.Unit;
        if (unitSettings == null)
        {
            Debug.LogWarning("[UnitDefinitionSOImporter] Unit settings are not configured.");
            return;
        }

        var folderAsset = unitSettings.Folder;
        if (folderAsset == null)
        {
            Debug.LogWarning("[UnitDefinitionSOImporter] Target folder is not assigned.");
            return;
        }

        var folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[UnitDefinitionSOImporter] '{folderPath}' is not a valid folder.");
            return;
        }

        var abilitiesFolderAsset = unitSettings.AbilitiesFolder;
        if (abilitiesFolderAsset == null)
        {
            Debug.LogWarning("[UnitDefinitionSOImporter] Abilities folder is not assigned.");
            return;
        }

        var abilitiesFolderPath = AssetDatabase.GetAssetPath(abilitiesFolderAsset);
        if (string.IsNullOrEmpty(abilitiesFolderPath) || !AssetDatabase.IsValidFolder(abilitiesFolderPath))
        {
            Debug.LogWarning($"[UnitDefinitionSOImporter] '{abilitiesFolderPath}' is not a valid abilities folder.");
            return;
        }

        var spriteLookup = BuildSpriteLookup(unitSettings.Sprites);
        var abilities = LoadAbilities(abilitiesFolderPath);
        if (abilities.Count == 0)
        {
            Debug.LogWarning($"[UnitDefinitionSOImporter] No BattleAbilityDefinitionSO assets were found in '{abilitiesFolderPath}'.");
        }

        foreach (var column in RequiredColumns)
        {
            if (!sheet.Headers.Any(header => string.Equals(header, column, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning($"[UnitDefinitionSOImporter] Sheet '{sheet.Name}' is missing required column '{column}'.");
                return;
            }
        }

        foreach (var row in sheet.Rows)
        {
            var id = row.GetValueOrDefault("ID");
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[UnitDefinitionSOImporter] Row {row.RowNumber} skipped: ID is empty.");
                continue;
            }

            var assetFileName = SanitizeFileName(id);
            if (string.IsNullOrEmpty(assetFileName))
            {
                Debug.LogWarning($"[UnitDefinitionSOImporter] Row {row.RowNumber} skipped: ID '{id}' is not a valid file name.");
                continue;
            }

            var assetPath = Path.Combine(folderPath, assetFileName + ".asset").Replace('\\', '/');
            var unit = AssetDatabase.LoadAssetAtPath<UnitDefinitionSO>(assetPath);
            if (unit == null)
            {
                unit = ScriptableObject.CreateInstance<UnitDefinitionSO>();
                AssetDatabase.CreateAsset(unit, assetPath);
            }

            unit.UnitName = row.GetValueOrDefault("UnitName");

            var iconValue = row.GetValueOrDefault("Icon");
            if (TryResolveSprite(iconValue, spriteLookup, row.RowNumber, out var sprite))
            {
                unit.Icon = sprite;
            }
            else
            {
                unit.Icon = null;
            }

            var kindValue = row.GetValueOrDefault("Kind");
            if (Enum.TryParse(kindValue, true, out UnitKind kind))
            {
                unit.Kind = kind;
            }
            else if (!string.IsNullOrWhiteSpace(kindValue))
            {
                Debug.LogWarning($"[UnitDefinitionSOImporter] Row {row.RowNumber}: Kind '{kindValue}' is invalid. Defaulting to Neutral.");
                unit.Kind = UnitKind.Neutral;
            }
            else
            {
                unit.Kind = UnitKind.Neutral;
            }

            var attackKindValue = row.GetValueOrDefault("AttackKind");
            if (Enum.TryParse(attackKindValue, true, out AttackKind attackKind))
            {
                unit.AttackKind = attackKind;
            }
            else if (!string.IsNullOrWhiteSpace(attackKindValue))
            {
                Debug.LogWarning($"[UnitDefinitionSOImporter] Row {row.RowNumber}: AttackKind '{attackKindValue}' is invalid. Defaulting to Melee.");
                unit.AttackKind = AttackKind.Melee;
            }
            else
            {
                unit.AttackKind = AttackKind.Melee;
            }

            var damageTypeValue = row.GetValueOrDefault("DamageType");
            if (Enum.TryParse(damageTypeValue, true, out DamageType damageType))
            {
                unit.DamageType = damageType;
            }
            else if (!string.IsNullOrWhiteSpace(damageTypeValue))
            {
                Debug.LogWarning($"[UnitDefinitionSOImporter] Row {row.RowNumber}: DamageType '{damageTypeValue}' is invalid. Defaulting to Physical.");
                unit.DamageType = DamageType.Physical;
            }
            else
            {
                unit.DamageType = DamageType.Physical;
            }

            unit.BaseHealth = ParseFloat(row.GetValueOrDefault("BaseHealth"), row.RowNumber, "BaseHealth");
            unit.BasePhysicalDefense = ParseFloat(row.GetValueOrDefault("BasePhysicalDefense"), row.RowNumber, "BasePhysicalDefense");
            unit.BaseMagicDefense = ParseFloat(row.GetValueOrDefault("BaseMagicDefense"), row.RowNumber, "BaseMagicDefense");
            unit.BaseAbsoluteDefense = ParseFloat(row.GetValueOrDefault("BaseAbsoluteDefense"), row.RowNumber, "BaseAbsoluteDefense");
            unit.MinDamage = ParseFloat(row.GetValueOrDefault("MinDamage"), row.RowNumber, "MinDamage");
            unit.MaxDamage = ParseFloat(row.GetValueOrDefault("MaxDamage"), row.RowNumber, "MaxDamage");
            unit.Speed = ParseFloat(row.GetValueOrDefault("Speed"), row.RowNumber, "Speed");
            unit.BaseCritChance = ParseFloat(row.GetValueOrDefault("BaseCritChance"), row.RowNumber, "BaseCritChance");
            unit.BaseCritMultiplier = ParseFloat(row.GetValueOrDefault("BaseCritMultiplier"), row.RowNumber, "BaseCritMultiplier");
            unit.BaseMissChance = ParseFloat(row.GetValueOrDefault("BaseMissChance"), row.RowNumber, "BaseMissChance");

            var abilitiesValue = row.GetValueOrDefault("Abilities");
            if (!string.IsNullOrWhiteSpace(abilitiesValue))
            {
                var abilityNames = abilitiesValue
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(name => name.Trim())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();

                var resolvedAbilities = new List<BattleAbilityDefinitionSO>();
                foreach (var abilityName in abilityNames)
                {
                    var lookupKey = SanitizeFileName(abilityName);
                    if (string.IsNullOrEmpty(lookupKey))
                    {
                        Debug.LogWarning($"[UnitDefinitionSOImporter] Row {row.RowNumber}: Ability '{abilityName}' is not a valid reference.");
                        continue;
                    }

                    if (abilities.TryGetValue(lookupKey, out var ability))
                    {
                        resolvedAbilities.Add(ability);
                    }
                    else
                    {
                        Debug.LogWarning($"[UnitDefinitionSOImporter] Row {row.RowNumber}: Ability '{abilityName}' not found in abilities folder '{abilitiesFolderPath}'.");
                    }
                }

                unit.Abilities = resolvedAbilities.ToArray();
            }
            else
            {
                unit.Abilities = Array.Empty<BattleAbilityDefinitionSO>();
            }

            EditorUtility.SetDirty(unit);
        }

        AssetDatabase.SaveAssets();
    }

    private static Dictionary<string, BattleAbilityDefinitionSO> LoadAbilities(string folderPath)
    {
        var result = new Dictionary<string, BattleAbilityDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        var guids = AssetDatabase.FindAssets("t:BattleAbilityDefinitionSO", new[] { folderPath });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ability = AssetDatabase.LoadAssetAtPath<BattleAbilityDefinitionSO>(path);
            if (ability == null)
            {
                continue;
            }

            var fileName = Path.GetFileNameWithoutExtension(path);
            AddAbility(result, fileName, ability, folderPath);

            if (!string.IsNullOrWhiteSpace(ability.Id))
            {
                AddAbility(result, ability.Id, ability, folderPath);
            }
        }

        return result;
    }

    private static void AddAbility(Dictionary<string, BattleAbilityDefinitionSO> abilities, string key, BattleAbilityDefinitionSO ability, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var sanitized = SanitizeFileName(key);
        if (string.IsNullOrEmpty(sanitized))
        {
            return;
        }

        if (abilities.TryGetValue(sanitized, out var existing))
        {
            if (existing == ability)
            {
                return;
            }

            Debug.LogWarning($"[UnitDefinitionSOImporter] Duplicate ability '{sanitized}' found in '{folderPath}'. Using the first occurrence.");
            return;
        }

        abilities[sanitized] = ability;
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

        Debug.LogWarning($"[UnitDefinitionSOImporter] Row {rowNumber}: Icon '{value}' not found in configured sprites.");
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
                Debug.LogWarning($"[UnitDefinitionSOImporter] Sprites array contains an unassigned entry at index {i}.");
                continue;
            }

            if (result.ContainsKey(sprite.name))
            {
                Debug.LogWarning($"[UnitDefinitionSOImporter] Duplicate sprite name '{sprite.name}' found. Using the first occurrence.");
                continue;
            }

            result[sprite.name] = sprite;
        }

        return result;
    }

    private static float ParseFloat(string value, int rowNumber, string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0f;
        }

        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedFloat))
        {
            return parsedFloat;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
        {
            return parsedInt;
        }

        Debug.LogWarning($"[UnitDefinitionSOImporter] Row {rowNumber}: Unable to parse '{columnName}' value '{value}'. Using 0.");
        return 0f;
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
