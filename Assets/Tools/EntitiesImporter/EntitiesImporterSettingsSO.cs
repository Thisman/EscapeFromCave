using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "EntitiesImporterSettings",
    menuName = "Tools/Entities Importer Settings",
    order = 0)]
public class EntitiesImporterSettingsSO : ScriptableObject
{
    private const string DefaultDelimiter = ",";

    [Header("General")]
    [SerializeField] private string interactionEffectsTableUrl;
    [SerializeField] private string interactionConditionsTableUrl;
    [SerializeField] private string interactionTargetResolversTableUrl;
    [SerializeField] private string interactionsTableUrl;
    [SerializeField] private string objectsTableUrl;
    [SerializeField] private string battleEffectsTableUrl;
    [SerializeField] private string battleAbilitiesTableUrl;
    [SerializeField] private string unitsTableUrl;
    [SerializeField] private string delimiter = DefaultDelimiter;

    [Header("Battle Effects")]
#if UNITY_EDITOR
    [SerializeField] private DefaultAsset battleEffectsOutputFolder;
#else
    [SerializeField] private UnityEngine.Object battleEffectsOutputFolder;
#endif
    [SerializeField] private Sprite[] battleEffectIcons = Array.Empty<Sprite>();

    public string InteractionEffectsTableUrl => interactionEffectsTableUrl;
    public string InteractionConditionsTableUrl => interactionConditionsTableUrl;
    public string InteractionTargetResolversTableUrl => interactionTargetResolversTableUrl;
    public string InteractionsTableUrl => interactionsTableUrl;
    public string ObjectsTableUrl => objectsTableUrl;
    public string BattleEffectsTableUrl => battleEffectsTableUrl;
    public string BattleAbilitiesTableUrl => battleAbilitiesTableUrl;
    public string UnitsTableUrl => unitsTableUrl;
    public string Delimiter => string.IsNullOrEmpty(delimiter) ? DefaultDelimiter : delimiter;
    public IReadOnlyList<Sprite> BattleEffectIcons => battleEffectIcons ?? Array.Empty<Sprite>();

#if UNITY_EDITOR
    public DefaultAsset BattleEffectsOutputFolder => battleEffectsOutputFolder;
#endif

    public string GetBattleEffectsOutputFolderPath()
    {
#if UNITY_EDITOR
        if (battleEffectsOutputFolder == null)
        {
            return string.Empty;
        }

        var path = AssetDatabase.GetAssetPath(battleEffectsOutputFolder);
        if (!AssetDatabase.IsValidFolder(path))
        {
            return string.Empty;
        }

        return path;
#else
        return string.Empty;
#endif
    }

    public IEnumerable<string> GetAllTableUrls()
    {
        yield return interactionEffectsTableUrl;
        yield return interactionConditionsTableUrl;
        yield return interactionTargetResolversTableUrl;
        yield return interactionsTableUrl;
        yield return objectsTableUrl;
        yield return battleEffectsTableUrl;
        yield return battleAbilitiesTableUrl;
        yield return unitsTableUrl;
    }
}
