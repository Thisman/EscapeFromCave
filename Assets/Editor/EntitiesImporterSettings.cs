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

    public IReadOnlyList<DefaultAsset> Tables => tables;

    public BattleEffectImportSettings BattleEffect => battleEffect;
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
