using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Abilities Import Settings")]
public sealed class AbilitiesImportSettingsSO : ScriptableObject
{
    [Header("Источник данных")]
    public TextAsset Table;         // CSV/TSV (UTF-8)
    public char Delimiter = ',';    // CSV = ',' ; TSV = '\t'
    public bool HasHeader = true;

    [Header("Куда складывать ассеты (ОЧИСТИТСЯ перед импортом)")]
    public DefaultAsset RootFolder;

    [Header("Иконки (индексация по столбцу Icon)")]
    public Sprite[] Sprites;

    [Header("Где лежат эффекты (будем искать по имени файла)")]
    public DefaultAsset EffectsFolder; // Папка с BattleEffectDefinitionSO
}
