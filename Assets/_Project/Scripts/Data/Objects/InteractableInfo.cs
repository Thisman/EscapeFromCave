using System;
using UnityEngine;

[Serializable]
public struct InteractableInfo
{
    public string DisplayName;          // "Сундук", "Рычаг", "Алтарь"
    public string Description;          // "Открыть", "Активировать", "Помолиться"
    public Sprite Icon;                 // иконка для UI
    public InteractionType Type;        // тип (Use, Pickup, Talk, Examine и т.п.)
    public bool RequiresCondition;      // есть ли условие (ключ, энергия, квест)
    public float InteractionDistance;   // радиус/дистанция активации
    public Color HighlightColor;        // цвет подсветки при наведении
}
