using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "EntitiesImporterSettings", menuName = "Tools/Entities Importer Settings")]
public class EntitiesImporterSettings : ScriptableObject
{
    [SerializeField]
    private DefaultAsset[] tables = Array.Empty<DefaultAsset>();

    [SerializeField]
    private BattleEffectImportSettings battleEffect = new();

    [SerializeField]
    private BattleAbilityImportSettings battleAbility = new();

    [SerializeField]
    private UnitImportSettings unit = new();

    public IReadOnlyList<DefaultAsset> Tables => tables;

    public BattleEffectImportSettings BattleEffect => battleEffect;

    public BattleAbilityImportSettings BattleAbility => battleAbility;

    public UnitImportSettings Unit => unit;
}

[Serializable]
public sealed class BattleEffectImportSettings
{
    [SerializeField]
    private DefaultAsset folder;

    [SerializeField]
    private Sprite[] sprites = Array.Empty<Sprite>();

    public DefaultAsset Folder => folder;

    public IReadOnlyList<Sprite> Sprites => sprites ?? Array.Empty<Sprite>();
}

[Serializable]
public sealed class BattleAbilityImportSettings
{
    [SerializeField]
    private DefaultAsset folder;

    [SerializeField]
    private Sprite[] sprites = Array.Empty<Sprite>();

    [SerializeField]
    private DefaultAsset effectsFolder;

    public DefaultAsset Folder => folder;

    public IReadOnlyList<Sprite> Sprites => sprites ?? Array.Empty<Sprite>();

    public DefaultAsset EffectsFolder => effectsFolder;
}

[Serializable]
public sealed class UnitImportSettings
{
    [SerializeField]
    private DefaultAsset folder;

    [SerializeField]
    private DefaultAsset abilitiesFolder;

    [SerializeField]
    private Sprite[] sprites = Array.Empty<Sprite>();

    public DefaultAsset Folder => folder;

    public DefaultAsset AbilitiesFolder => abilitiesFolder;

    public IReadOnlyList<Sprite> Sprites => sprites ?? Array.Empty<Sprite>();
}
