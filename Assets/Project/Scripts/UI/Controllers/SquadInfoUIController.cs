using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class SquadInfoUIController : MonoBehaviour
{
    private const float StatFontSize = 20f;
    private const float StatWidth = 400f;
    [SerializeField] private GameObject _friendlyPanel;
    [SerializeField] private GameObject _enemyPanel;

    private Transform _friendlyContainer;
    private Transform _enemyContainer;
    private IReadOnlySquadModel _currentModel;

    private void Awake()
    {
        _friendlyContainer = ResolveContainer(_friendlyPanel);
        _enemyContainer = ResolveContainer(_enemyPanel);
        Hide();
    }

    public void Render(IReadOnlySquadModel squadModel)
    {
        if (!ReferenceEquals(_currentModel, squadModel))
        {
            SubscribeToModel(squadModel);
        }

        if (_currentModel == null || IsModelDestroyed(_currentModel))
        {
            HidePanels();
            return;
        }

        RenderCurrentModel();
    }

    public void Hide()
    {
        HidePanels();
        SubscribeToModel(null);
    }

    private void OnDestroy()
    {
        SubscribeToModel(null);
    }

    private void RenderCurrentModel()
    {
        if (_currentModel == null)
        {
            HidePanels();
            return;
        }

        var panel = ResolveTargetPanel(_currentModel, out var container);
        if (panel == null || container == null)
        {
            HidePanels();
            return;
        }

        ShowExclusivePanel(panel);
        PopulatePanel(container, _currentModel);
    }

    private void HidePanels()
    {
        HidePanel(_friendlyPanel, _friendlyContainer);
        HidePanel(_enemyPanel, _enemyContainer);
    }

    private void SubscribeToModel(IReadOnlySquadModel newModel)
    {
        if (_currentModel != null)
            _currentModel.Changed -= HandleModelChanged;

        _currentModel = newModel;

        if (_currentModel == null || IsModelDestroyed(_currentModel))
        {
            _currentModel = null;
            return;
        }

        _currentModel.Changed += HandleModelChanged;
    }

    private void HandleModelChanged(IReadOnlySquadModel model)
    {
        if (!ReferenceEquals(model, _currentModel))
            return;

        if (_currentModel == null || IsModelDestroyed(_currentModel))
        {
            Hide();
            return;
        }

        RenderCurrentModel();
    }

    private void PopulatePanel(Transform container, IReadOnlySquadModel squadModel)
    {
        ClearContainer(container);

        var entries = BuildEntries(squadModel);
        for (int i = 0; i < entries.Count; i++)
        {
            CreateText(container, entries[i]);
        }
    }

    private static IReadOnlyList<string> BuildEntries(IReadOnlySquadModel model)
    {
        var entries = new List<string>
        {
            $"Название: {model.UnitName}",
            $"Количество: {model.Count}",
            $"Здоровье: {FormatValue(model.Health)}",
            $"Физическая защита: {FormatPercent(model.PhysicalDefense)}",
            $"Магическая защита: {FormatPercent(model.MagicDefense)}",
            $"Абсолютная защита: {FormatPercent(model.AbsoluteDefense)}"
        };

        entries.Add($"Тип атаки: {FormatAttackKind(model.AttackKind)}");
        entries.Add($"Тип урона: {FormatDamageType(model.DamageType)}");

        var (min, max) = model.GetBaseDamageRange();
        entries.Add($"Урон: {FormatValue(min)} - {FormatValue(max)}");
        entries.Add($"Скорость: {FormatValue(model.Speed)}");
        entries.Add($"Инициатива: {FormatValue(model.Initiative)}");
        entries.Add($"Шанс критического удара: {FormatPercent(model.CritChance)}");
        entries.Add($"Критический множитель: {FormatValue(model.CritMultiplier)}");
        entries.Add($"Шанс промаха: {FormatPercent(model.MissChance)}");

        return entries;
    }

    private void ShowExclusivePanel(GameObject panelToShow)
    {
        if (_friendlyPanel != null)
            _friendlyPanel.SetActive(panelToShow == _friendlyPanel);
        if (_enemyPanel != null)
            _enemyPanel.SetActive(panelToShow == _enemyPanel);
    }

    private void HidePanel(GameObject panel, Transform container)
    {
        if (panel != null)
            panel.SetActive(false);
        if (container != null)
            ClearContainer(container);
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private void CreateText(Transform container, string content)
    {
        if (container == null)
            return;

        var go = new GameObject("Stat", typeof(RectTransform));
        go.transform.SetParent(container, false);

        var rectTransform = go.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(StatWidth, rectTransform.sizeDelta.y);
        rectTransform.anchoredPosition = new Vector2(0f, rectTransform.anchoredPosition.y);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = StatFontSize;
    }

    private GameObject ResolveTargetPanel(IReadOnlySquadModel model, out Transform container)
    {
        bool isEnemy = model.IsEnemy();

        if (isEnemy)
        {
            container = _enemyContainer;
            return _enemyPanel;
        }

        container = _friendlyContainer;
        return _friendlyPanel;
    }

    private static Transform ResolveContainer(GameObject panel)
    {
        if (panel == null)
            return null;

        var containerTransform = panel.transform.Find("Container");
        if (containerTransform == null)
        {
            Debug.LogWarning($"[{nameof(SquadInfoUIController)}.{nameof(ResolveContainer)}] Container transform was not found for '{panel.name}'.");
        }

        return containerTransform;
    }

    private static bool IsModelDestroyed(IReadOnlySquadModel model)
    {
        if (model is Object unityObject)
            return unityObject == null;

        return false;
    }

    private static string FormatValue(float value)
    {
        return value.ToString("0.##");
    }

    private static string FormatPercent(float value)
    {
        return value.ToString("P0");
    }

    private static string FormatAttackKind(AttackKind attackKind)
    {
        return attackKind switch
        {
            AttackKind.Melee => "Ближняя",
            AttackKind.Range => "Дальняя",
            AttackKind.Magic => "Магическая",
            _ => attackKind.ToString()
        };
    }

    private static string FormatDamageType(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => "Физический",
            DamageType.Magical => "Магический",
            DamageType.Pure => "Чистый",
            _ => damageType.ToString()
        };
    }
}
