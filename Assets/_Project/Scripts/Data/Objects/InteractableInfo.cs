using System;
using UnityEngine;

[Serializable]
public struct InteractableInfo
{
    public string DisplayName;          // "������", "�����", "������"
    public string Description;          // "�������", "������������", "����������"
    public Sprite Icon;                 // ������ ��� UI
    public InteractionType Type;        // ��� (Use, Pickup, Talk, Examine � �.�.)
    public bool RequiresCondition;      // ���� �� ������� (����, �������, �����)
    public float InteractionDistance;   // ������/��������� ���������
    public Color HighlightColor;        // ���� ��������� ��� ���������
}
