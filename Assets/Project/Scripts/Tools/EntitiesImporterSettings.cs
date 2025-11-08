using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntitiesImporterSettings", menuName = "Tools/Entities Importer Settings")]
public class EntitiesImporterSettings : ScriptableObject
{
    [SerializeField]
    private TextAsset[] tables = new TextAsset[0];

    public IReadOnlyList<TextAsset> Tables => tables;
}
