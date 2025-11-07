using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BattleEffectsCreator : BaseEntityCreator
{
    private readonly EntitiesImporterSettingsSO _settings;
    private readonly Dictionary<string, Sprite> _iconLookup;

    public BattleEffectsCreator(EntitiesImporterSettingsSO settings)
    {
        _settings = settings;
        _iconLookup = BuildIconLookup(settings);
    }

    public override void Create(IEntityTableData data)
    {
        if (data is not BattleEffectTableData battleEffectData)
        {
            Debug.LogWarning("BattleEffectsCreator: Unsupported data passed to creator.");
            return;
        }

        if (_settings == null)
        {
            Debug.LogWarning("BattleEffectsCreator: Settings are not configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(battleEffectData.Id))
        {
            Debug.LogWarning("BattleEffectsCreator: Battle effect ID is empty.");
            return;
        }

        var outputFolder = _settings.BattleEffectsOutputFolder;
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogWarning("BattleEffectsCreator: Output folder is not specified in settings.");
            return;
        }

        if (!EnsureFolder(outputFolder))
        {
            Debug.LogError($"BattleEffectsCreator: Failed to validate or create folder '{outputFolder}'.");
            return;
        }

        switch (battleEffectData.SheetName)
        {
            case nameof(DamageBattleEffect):
                CreateOrUpdateDamageEffect(outputFolder, battleEffectData);
                break;
            case nameof(StatModifierBattleEffect):
                CreateOrUpdateStatModifierEffect(outputFolder, battleEffectData);
                break;
            default:
                Debug.LogWarning($"BattleEffectsCreator: Unsupported sheet '{battleEffectData.SheetName}'.");
                break;
        }
    }

    private void CreateOrUpdateDamageEffect(string folder, BattleEffectTableData data)
    {
        var assetPath = ComposeAssetPath(folder, data.Id);
        var asset = LoadOrCreate<DamageBattleEffect>(assetPath);
        if (asset == null)
        {
            Debug.LogError($"BattleEffectsCreator: Could not create asset for '{data.Id}'.");
            return;
        }

        ApplyCommonFields(asset, data);

        if (data.TryGetInt("Damage", out var damage))
        {
            asset.Damage = Mathf.Max(0, damage);
        }

        FinalizeAsset(asset, data.Id);
    }

    private void CreateOrUpdateStatModifierEffect(string folder, BattleEffectTableData data)
    {
        var assetPath = ComposeAssetPath(folder, data.Id);
        var asset = LoadOrCreate<StatModifierBattleEffect>(assetPath);
        if (asset == null)
        {
            Debug.LogError($"BattleEffectsCreator: Could not create asset for '{data.Id}'.");
            return;
        }

        ApplyCommonFields(asset, data);
        asset.StatsModifiers = data.StatsModifiers?.ToArray() ?? Array.Empty<BattleStatModifier>();

        FinalizeAsset(asset, data.Id);
    }

    private void ApplyCommonFields(BattleEffectDefinitionSO asset, BattleEffectTableData data)
    {
        asset.Name = data.Name;
        asset.Description = data.Description;
        asset.Icon = ResolveIcon(data.IconKey);
        asset.Trigger = ParseTrigger(data.Trigger);

        if (data.TryGetInt("MaxTick", out var maxTick))
        {
            asset.MaxTick = Mathf.Max(0, maxTick);
        }
    }

    private void FinalizeAsset(ScriptableObject asset, string displayName)
    {
        asset.name = displayName;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        var existingType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
        if (existingType != null && existingType != typeof(T))
        {
            Debug.LogError($"BattleEffectsCreator: Asset at '{assetPath}' has type {existingType.Name} but expected {typeof(T).Name}.");
            return null;
        }

        var instance = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(instance, assetPath);
        return instance;
    }

    private Sprite ResolveIcon(string iconKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            return null;
        }

        var key = iconKey.Trim();

        if (_iconLookup.TryGetValue(key, out var sprite))
        {
            return sprite;
        }

        return null;
    }

    private static bool EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return true;
        }

        var segments = folder.Split('/');
        if (segments.Length == 0 || !string.Equals(segments[0], "Assets", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("BattleEffectsCreator: Output folder must be inside the Assets directory.");
            return false;
        }

        var currentPath = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            var nextPath = $"{currentPath}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }

        return AssetDatabase.IsValidFolder(folder);
    }

    private static Dictionary<string, Sprite> BuildIconLookup(EntitiesImporterSettingsSO settings)
    {
        var lookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        if (settings?.BattleEffectIcons == null)
        {
            return lookup;
        }

        foreach (var sprite in settings.BattleEffectIcons)
        {
            if (sprite == null)
            {
                continue;
            }

            lookup[sprite.name] = sprite;
        }

        return lookup;
    }

    private static BattleEffectTrigger ParseTrigger(string triggerValue)
    {
        if (!string.IsNullOrWhiteSpace(triggerValue) &&
            Enum.TryParse(triggerValue, true, out BattleEffectTrigger trigger))
        {
            return trigger;
        }

        return BattleEffectTrigger.OnAttach;
    }

    private static string ComposeAssetPath(string folder, string id)
    {
        return $"{folder.TrimEnd('/')}/{id}.asset";
    }
}
