using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromCave.EditorTools.EntitiesImporter
{
    [CreateAssetMenu(fileName = "EntitiesImporterSettings", menuName = "Entities/Importer Settings")]
    public class EntitiesImporterSettingsSO : ScriptableObject
    {
        [SerializeField]
        private List<string> tableLinks = new List<string>();

        [SerializeField]
        private string delimiter = ",";

        public IReadOnlyList<string> TableLinks => tableLinks;

        public char Delimiter => string.IsNullOrEmpty(delimiter) ? ',' : delimiter[0];
    }
}
