using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public sealed class DangeonLevelSceneUIController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private readonly List<SquadIconEntry> _squadIcons = new();
    private IReadOnlyArmyModel _currentArmy;
    private IReadOnlySquadModel _currentDisplayedModel;
    private IReadOnlySquadModel _subscribedModel;
    private bool _initialized;

    private VisualElement _root;
    private VisualElement _squadsContainer;
    private VisualElement _squadInfoPanel;
    private VisualElement _enemyInfoPanel;
    private VisualElement _dialogContainer;
    private Label _dialogLabel;

    private void Awake()
    {
        Initialize();
        HideSquadInfo();
        HideDialog();
    }

    private void OnEnable()
    {
        Initialize();
        InputSystem.onAfterUpdate += HandleAfterInputUpdate;
    }

    private void OnDisable()
    {
        InputSystem.onAfterUpdate -= HandleAfterInputUpdate;
        HideSquadInfo();
    }

    private void OnDestroy()
    {
        SubscribeToArmy(null);
        InputSystem.onAfterUpdate -= HandleAfterInputUpdate;
        UpdateModelSubscription(null);
    }

    public void RenderArmy(IReadOnlyArmyModel army)
    {
        Initialize();

        if (!ReferenceEquals(_currentArmy, army))
        {
            SubscribeToArmy(army);
        }

        UpdateSquadIcons();
    }

    public Label ShowDialog()
    {
        Initialize();

        if (_dialogContainer == null)
            return null;

        if (_dialogLabel == null)
        {
            _dialogLabel = new Label
            {
                name = "DialogLabel"
            };
            _dialogLabel.AddToClassList("dialogLabel");
            _dialogContainer.Add(_dialogLabel);
        }

        _dialogContainer.style.display = DisplayStyle.Flex;
        return _dialogLabel;
    }

    public void HideDialog()
    {
        if (_dialogLabel != null)
            _dialogLabel.text = string.Empty;

        if (_dialogContainer != null)
            _dialogContainer.style.display = DisplayStyle.None;
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _document ??= GetComponent<UIDocument>();
        if (_document == null)
            return;

        _root = _document.rootVisualElement;
        if (_root == null)
            return;

        _squadsContainer = _root.Q<VisualElement>("SquadsContainer");
        _squadInfoPanel = _root.Q<VisualElement>("SquadInfoPanel");
        _enemyInfoPanel = _root.Q<VisualElement>("EnemyInfoPanel");
        _dialogContainer = _root.Q<VisualElement>("DialogContainer");

        _squadIcons.Clear();
        if (_squadsContainer != null)
        {
            foreach (VisualElement child in _squadsContainer.Children())
            {
                if (child == null)
                    continue;

                if (string.Equals(child.name, "SquadIcon", StringComparison.Ordinal))
                {
                    _squadIcons.Add(new SquadIconEntry(child));
                }
            }
        }

        if (_dialogContainer != null)
            _dialogContainer.style.display = DisplayStyle.None;

        HidePanel(_squadInfoPanel);
        HidePanel(_enemyInfoPanel);

        _initialized = true;
    }

    private void SubscribeToArmy(IReadOnlyArmyModel newArmy)
    {
        if (_currentArmy != null)
            _currentArmy.Changed -= HandleArmyChanged;

        _currentArmy = newArmy;

        if (_currentArmy != null)
            _currentArmy.Changed += HandleArmyChanged;
    }

    private void HandleArmyChanged(IReadOnlyArmyModel army)
    {
        if (!ReferenceEquals(army, _currentArmy))
            return;

        UpdateSquadIcons();
    }

    private void UpdateSquadIcons()
    {
        if (_squadsContainer == null)
            return;

        IReadOnlyList<IReadOnlySquadModel> squads = _currentArmy?.GetSquads();
        int required = squads?.Count ?? 0;
        EnsureSquadIconCapacity(required);

        for (int i = 0; i < _squadIcons.Count; i++)
        {
            IReadOnlySquadModel model = null;
            if (squads != null && i < squads.Count)
            {
                model = squads[i];
                if (model != null && model.IsEmpty)
                    model = null;
            }

            _squadIcons[i].SetModel(model);
        }
    }

    private void EnsureSquadIconCapacity(int required)
    {
        if (_squadsContainer == null)
            return;

        while (_squadIcons.Count < required)
        {
            var element = new VisualElement
            {
                name = "SquadIcon"
            };
            element.AddToClassList("squadIcon");
            _squadsContainer.Add(element);
            _squadIcons.Add(new SquadIconEntry(element));
        }
    }

    private void HandleAfterInputUpdate()
    {
        var model = ResolveHoveredSquadModel();

        if (ReferenceEquals(model, _currentDisplayedModel))
        {
            if (model != null && IsModelDestroyed(model))
            {
                HideSquadInfo();
            }

            return;
        }

        if (model == null)
        {
            HideSquadInfo();
            return;
        }

        if (IsModelDestroyed(model))
        {
            HideSquadInfo();
            return;
        }

        RenderSquadInfo(model);
    }

    private IReadOnlySquadModel ResolveHoveredSquadModel()
    {
        if (!TryGetPointerScreenPosition(out var screenPosition))
            return null;

        var uiModel = FindModelOnUI(screenPosition);
        if (uiModel != null)
            return uiModel;

        var provider = FindProviderInWorld(screenPosition);
        return provider?.GetSquadModel();
    }

    private IReadOnlySquadModel FindModelOnUI(Vector2 screenPosition)
    {
        var panel = _root?.panel;
        if (panel == null)
            return null;

        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        VisualElement picked = panel.Pick(panelPosition);

        while (picked != null)
        {
            if (picked.userData is IReadOnlySquadModel model && model != null && !model.IsEmpty)
                return model;

            picked = picked.parent;
        }

        return null;
    }

    private static ISquadModelProvider FindProviderInWorld(Vector2 screenPosition)
    {
        var camera = Camera.main;
        if (camera == null)
            return null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        var provider = FindProviderInWorld2D(ray);
        if (provider != null)
            return provider;

        return FindProviderInWorld3D(ray);
    }

    private static ISquadModelProvider FindProviderInWorld2D(Ray ray)
    {
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, 1000f);
        for (int i = 0; i < hits.Length; i++)
        {
            var transform = hits[i].transform;
            if (transform == null)
                continue;

            var provider = transform.GetComponentInParent<ISquadModelProvider>();
            if (provider != null)
                return provider;
        }

        return null;
    }

    private static ISquadModelProvider FindProviderInWorld3D(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        for (int i = 0; i < hits.Length; i++)
        {
            var transform = hits[i].transform;
            if (transform == null)
                continue;

            var provider = transform.GetComponentInParent<ISquadModelProvider>();
            if (provider != null)
                return provider;
        }

        return null;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 position)
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            position = default;
            return false;
        }

        position = mouse.position.ReadValue();
        return true;
    }

    private void RenderSquadInfo(IReadOnlySquadModel model)
    {
        _currentDisplayedModel = model;
        UpdateModelSubscription(model);

        VisualElement targetPanel = model.IsEnemy() ? _enemyInfoPanel : _squadInfoPanel;
        VisualElement oppositePanel = model.IsEnemy() ? _squadInfoPanel : _enemyInfoPanel;

        if (oppositePanel != null)
            HidePanel(oppositePanel);

        if (targetPanel == null)
            return;

        targetPanel.style.display = DisplayStyle.Flex;
        targetPanel.Clear();

        foreach (string entry in BuildEntries(model))
        {
            var label = new Label(entry)
            {
                name = "SquadInfoEntry"
            };
            targetPanel.Add(label);
        }
    }

    private void HideSquadInfo()
    {
        _currentDisplayedModel = null;
        UpdateModelSubscription(null);
        HidePanel(_squadInfoPanel);
        HidePanel(_enemyInfoPanel);
    }

    private static void HidePanel(VisualElement panel)
    {
        if (panel == null)
            return;

        panel.Clear();
        panel.style.display = DisplayStyle.None;
    }

    private void UpdateModelSubscription(IReadOnlySquadModel model)
    {
        if (ReferenceEquals(_subscribedModel, model))
            return;

        if (_subscribedModel != null)
            _subscribedModel.Changed -= HandleSquadModelChanged;

        _subscribedModel = model;

        if (_subscribedModel != null)
            _subscribedModel.Changed += HandleSquadModelChanged;
    }

    private void HandleSquadModelChanged(IReadOnlySquadModel model)
    {
        if (!ReferenceEquals(model, _subscribedModel))
            return;

        if (model == null || model.IsEmpty || IsModelDestroyed(model))
        {
            HideSquadInfo();
            return;
        }

        RenderSquadInfo(model);
    }

    private static bool IsModelDestroyed(IReadOnlySquadModel model)
    {
        if (model is UnityEngine.Object unityObject)
            return unityObject == null;

        return false;
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
            $"Абсолютная защита: {FormatPercent(model.AbsoluteDefense)}",
            $"Тип атаки: {FormatAttackKind(model.AttackKind)}",
            $"Тип урона: {FormatDamageType(model.DamageType)}"
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

    private sealed class SquadIconEntry
    {
        public SquadIconEntry(VisualElement element)
        {
            Element = element;
        }

        public VisualElement Element { get; }

        public void SetModel(IReadOnlySquadModel model)
        {
            Element.userData = model;

            if (model == null)
            {
                Element.tooltip = string.Empty;
                Element.style.display = DisplayStyle.None;
                Element.style.backgroundImage = new StyleBackground();
                return;
            }

            Element.tooltip = $"{model.UnitName} ({model.Count})";
            Element.style.display = DisplayStyle.Flex;

            if (model.Icon != null)
                Element.style.backgroundImage = new StyleBackground(model.Icon);
            else
                Element.style.backgroundImage = new StyleBackground();
        }
    }
}
