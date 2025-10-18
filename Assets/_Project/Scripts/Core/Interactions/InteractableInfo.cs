using System;
using UnityEngine;

[Serializable]
public struct InteractableInfo
{
    public string DisplayName;

    public string Description;

    public InteractionType Type;

    public bool RequiresCondition;

    public float InteractionDistance;

    public Color HighlightColor;
}
