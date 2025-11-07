using System.Collections.Generic;
using UnityEngine;

namespace Tools.EntitiesImporter
{
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

        public string InteractionEffectsTableUrl => interactionEffectsTableUrl;
        public string InteractionConditionsTableUrl => interactionConditionsTableUrl;
        public string InteractionTargetResolversTableUrl => interactionTargetResolversTableUrl;
        public string InteractionsTableUrl => interactionsTableUrl;
        public string ObjectsTableUrl => objectsTableUrl;
        public string BattleEffectsTableUrl => battleEffectsTableUrl;
        public string BattleAbilitiesTableUrl => battleAbilitiesTableUrl;
        public string UnitsTableUrl => unitsTableUrl;
        public string Delimiter => string.IsNullOrEmpty(delimiter) ? DefaultDelimiter : delimiter;

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
}
