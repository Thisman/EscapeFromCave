using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Damage Effects Import Settings")]
public sealed class DamageEffectsImportSettingsSO : ScriptableObject
{
    [Header("Источник данных")]
    public TextAsset Table;        // CSV/TSV (UTF-8)
    public char Delimiter = ',';   // CSV = ',' ; TSV = '\t'
    public bool HasHeader = true;  // первая строка — заголовки

    [Header("Куда складывать ассеты (ОЧИСТИТСЯ перед импортом)")]
    public DefaultAsset RootFolder;

    [Header("Иконки (индексация по столбцу Icon)")]
    public Sprite[] Sprites;       // общий список спрайтов для эффектов
}
