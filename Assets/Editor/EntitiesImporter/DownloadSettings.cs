using UnityEngine;

public enum DelimiterMode
{
    AutoFromExtension,
    Comma,
    Semicolon,
    Tab,
    Custom
}

[CreateAssetMenu(menuName = "Tables/Download Settings", fileName = "DownloadSettings")]
public sealed class DownloadSettings : ScriptableObject
{
    [Tooltip("Ссылки: http(s)://, file://, абсолютный путь, путь внутри Assets, либо GUID TextAsset.")]
    public string[] links;

    [Header("Разделитель (на будущее)")]
    public DelimiterMode delimiterMode = DelimiterMode.AutoFromExtension;

    [Tooltip("Используется при Custom")]
    public string customDelimiter = ",";
}
