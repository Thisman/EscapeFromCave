using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class BattleSquadInfoUIController : MonoBehaviour
{
    private const float StatFontSize = 20f;
    private const float StatWidth = 400f;
    [SerializeField] private GameObject _friendlyPanel;
    [SerializeField] private GameObject _enemyPanel;
    [Header("Animation")]
    [SerializeField, Min(0f)] private float _wobbleFrequency = 1.2f;
    [SerializeField] private Vector3 _wobbleAngles = new Vector3(3f, 2f, 1.5f);

    private Transform _friendlyContainer;
    private Transform _enemyContainer;
    private Vector3 _friendlyBaseEuler;
    private bool _hasFriendlyBaseEuler;
    private Vector3 _enemyBaseEuler;
    private bool _hasEnemyBaseEuler;
    private float _animationTime;

    private void Awake()
    {
        _friendlyContainer = ResolveContainer(_friendlyPanel);
        _enemyContainer = ResolveContainer(_enemyPanel);
        CacheBaseRotations();
        Hide();
    }

    private void Update()
    {
        _animationTime += Time.deltaTime;

        AnimateContainer(_friendlyPanel, _friendlyContainer, _friendlyBaseEuler, _hasFriendlyBaseEuler, 0f);
        AnimateContainer(_enemyPanel, _enemyContainer, _enemyBaseEuler, _hasEnemyBaseEuler, Mathf.PI);
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
            $"Физическая защита: {FormatPercent(model.PhysicalDefense)}",
            $"Магическая защита: {FormatPercent(model.MagicDefense)}",
            $"Абсолютная защита: {FormatPercent(model.AbsoluteDefense)}"
        };

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
        {
            _friendlyPanel.SetActive(panelToShow == _friendlyPanel);
            if (_friendlyPanel.activeSelf == false)
                ResetContainerRotation(_friendlyContainer, _friendlyBaseEuler, _hasFriendlyBaseEuler);
        }
        if (_enemyPanel != null)
        {
            _enemyPanel.SetActive(panelToShow == _enemyPanel);
            if (_enemyPanel.activeSelf == false)
                ResetContainerRotation(_enemyContainer, _enemyBaseEuler, _hasEnemyBaseEuler);
        }
    }

    private void HidePanel(GameObject panel, Transform container)
    {
        if (panel != null)
            panel.SetActive(false);
        if (container != null)
            ClearContainer(container);
        ResetContainerRotation(container, GetBaseEuler(container), HasBaseRotation(container));
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
            Debug.LogWarning($"[{nameof(BattleSquadInfoUIController)}.{nameof(ResolveContainer)}] Container transform was not found for '{panel.name}'.");
        }

        return containerTransform;
    }

    private void CacheBaseRotations()
    {
        if (_friendlyContainer != null)
        {
            _friendlyBaseEuler = _friendlyContainer.localEulerAngles;
            _hasFriendlyBaseEuler = true;
        }
        if (_enemyContainer != null)
        {
            _enemyBaseEuler = _enemyContainer.localEulerAngles;
            _hasEnemyBaseEuler = true;
        }
    }

    private void AnimateContainer(GameObject panel, Transform container, Vector3 baseEuler, bool hasBase, float phaseOffset)
    {
        if (panel == null || container == null || !panel.activeInHierarchy || !hasBase)
            return;

        float t = _animationTime * _wobbleFrequency + phaseOffset;
        float tiltX = Mathf.Sin(t) * _wobbleAngles.x;
        float tiltY = Mathf.Sin(t * 0.8f + phaseOffset * 0.5f) * _wobbleAngles.y;
        float tiltZ = Mathf.Cos(t * 0.6f + phaseOffset * 0.25f) * _wobbleAngles.z;

        var euler = baseEuler + new Vector3(tiltX, tiltY, tiltZ);
        container.localRotation = Quaternion.Euler(euler);
    }

    private void ResetContainerRotation(Transform container, Vector3 baseEuler, bool hasBase)
    {
        if (container == null || !hasBase)
            return;

        container.localRotation = Quaternion.Euler(baseEuler);
    }

    private Vector3 GetBaseEuler(Transform container)
    {
        if (container == _friendlyContainer)
            return _friendlyBaseEuler;
        if (container == _enemyContainer)
            return _enemyBaseEuler;
        return Vector3.zero;
    }

    private bool HasBaseRotation(Transform container)
    {
        if (container == _friendlyContainer)
            return _hasFriendlyBaseEuler;
        if (container == _enemyContainer)
            return _hasEnemyBaseEuler;
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
}
