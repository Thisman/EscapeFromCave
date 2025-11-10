using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class BattleSquadInfoUIController : MonoBehaviour
{
    private const float StatFontSize = 20f;
    [SerializeField] private GameObject _friendlyPanel;
    [SerializeField] private GameObject _enemyPanel;

    private Transform _friendlyContainer;
    private Transform _enemyContainer;

    private void Awake()
    {
        _friendlyContainer = ResolveContainer(_friendlyPanel);
        _enemyContainer = ResolveContainer(_enemyPanel);
        Hide();
    }

    public void Render(IReadOnlySquadModel squadModel)
    {
        if (squadModel == null)
        {
            Hide();
            return;
        }

        var panel = ResolveTargetPanel(squadModel, out var container);
        if (panel == null || container == null)
        {
            Hide();
            return;
        }

        ShowExclusivePanel(panel);
        PopulatePanel(container, squadModel);
    }

    public void Hide()
    {
        HidePanel(_friendlyPanel, _friendlyContainer);
        HidePanel(_enemyPanel, _enemyContainer);
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
            $"Физическая защита: {FormatValue(model.PhysicalDefense)}",
            $"Магическая защита: {FormatValue(model.MagicDefense)}",
            $"Абсолютная защита: {FormatValue(model.AbsoluteDefense)}"
        };

        var damage = model.GetBaseDamageRange();
        entries.Add($"Урон: {FormatValue(damage.min)} - {FormatValue(damage.max)}");
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
            Debug.LogWarning($"[{nameof(BattleSquadInfoUIController)}.{nameof(ResolveContainer)}] Container transform was not found for '{panel.name}'.");
        }

        return containerTransform;
    }

    private static string FormatValue(float value)
    {
        return value.ToString("0.##");
    }

    private static string FormatPercent(float value)
    {
        return value.ToString("P0");
    }
}
