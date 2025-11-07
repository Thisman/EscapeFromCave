using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Abilities Import Settings")]
public sealed class AbilitiesImportSettingsSO : ScriptableObject
{
    [Header("Источник данных")]
    [Tooltip("Ссылка на Google-таблицу (можно обычную /edit; загрузка конвертируется в CSV/TSV)")]
    public string TableUrl;         // CSV/TSV (UTF-8)
    public char Delimiter = ',';    // CSV = ',' ; TSV = '\t'
    public bool HasHeader = true;

    [Header("Куда складывать ассеты (будет обновляться/создаваться)")]
    public DefaultAsset RootFolder;

    [Header("Иконки (индексация по столбцу Icon)")]
    public Sprite[] Sprites;

    [Header("Где лежат эффекты (будем искать по имени файла)")]
    public DefaultAsset EffectsFolder; // Папка с BattleEffectDefinitionSO
}
