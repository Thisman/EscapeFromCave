using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "EntitiesImporterSettings", menuName = "Tools/Entities Importer Settings")]
public class EntitiesImporterSettings : ScriptableObject
{
    [SerializeField]
    private DefaultAsset[] tables = Array.Empty<DefaultAsset>();

    public IReadOnlyList<DefaultAsset> Tables => tables;
}
