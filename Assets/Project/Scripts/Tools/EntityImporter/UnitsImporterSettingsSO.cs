using UnityEngine;
using UnityEditor;
using UnityEngine.U2D; // если SpriteAtlas

[CreateAssetMenu(menuName = "Tools/Units Import Settings")]
public sealed class UnitsImportSettingsSO : ScriptableObject
{
    [Header("Источник данных")]
    [Tooltip("Ссылка на опубликованную Google-таблицу (формат CSV/TSV)")]
    public string TableUrl;                  // CSV/TSV
    public char Delimiter = '\t';            // по умолчанию TSV для Google Sheets
    public bool HasHeader = true;
    public System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;

    [Header("Куда складывать ассеты (будет обновляться/создаваться)")]
    public DefaultAsset RootFolder;          // укажи папку в Project

    [Header("Иконки по UnitKind")]
    public Sprite[] AllySprites;
    public Sprite[] HeroSprites;
    public Sprite[] EnemySprites;
    public Sprite[] NeutralSprites;

    [Header("Нормализация процентов")]
    public bool AutoNormalizePercents = true; // если значение > 1 → делить на 100
}
