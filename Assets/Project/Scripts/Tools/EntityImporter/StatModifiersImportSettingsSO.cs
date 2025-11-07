using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Stat Modifiers Import Settings")]
public sealed class StatModifiersImportSettingsSO : ScriptableObject
{
    [Header("Источник данных")]
    [Tooltip("Ссылка на опубликованную Google-таблицу (формат CSV/TSV)")]
    public string TableUrl;        // CSV/TSV (UTF-8)
    public char Delimiter = ',';   // CSV = ',' ; TSV = '\t'
    public bool HasHeader = true;  // первая строка — заголовки

    [Header("Куда складывать ассеты (будет обновляться/создаваться)")]
    public DefaultAsset RootFolder;

    [Header("Иконки (индексация по столбцу Icon)")]
    public Sprite[] Sprites;       // общий список спрайтов для эффектов
}
