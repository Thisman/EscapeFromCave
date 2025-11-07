using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Objects Import Settings")]
public sealed class ObjectsImportSettingsSO : ScriptableObject
{
    [Header("Источник данных")]
    [Tooltip("Ссылка на опубликованную Google-таблицу (формат CSV/TSV)")]
    public string TableUrl;

    public char Delimiter = ',';   // CSV = ',' ; TSV = '\t'
    public bool HasHeader = true;  // первая строка — заголовки

    [Header("Куда складывать ассеты (будет обновляться/создаваться)")]
    public DefaultAsset RootFolder;

    [Header("Иконки (индексация по столбцу Icon)")]
    public Sprite[] Sprites;       // один общий список спрайтов для объектов
}
