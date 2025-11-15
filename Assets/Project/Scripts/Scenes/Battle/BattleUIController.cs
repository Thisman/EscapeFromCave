using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public sealed class BattleUIController : MonoBehaviour, ISceneUIController
{
    private const string BodyElementName = "Body";
    private const string StartCombatButtonName = "StartCombatButton";
    private const string LeaveCombatButtonName = "LeaveCombatButton";
    private const string FinishBattleButtonName = "FinishBattleButton";
    private const string ResultStatusLabelName = "ResultStatusLabel";
    private const string QueueListElementName = "QueueList";
    private const string AbilityListElementName = "AbilityList";

    private const string QueueItemClassName = "queue-item";
    private const string QueueItemIconClassName = "queue-item__icon";
    private const string QueueItemCountClassName = "queue-item__count";

    private const string AbilityItemClassName = "ability-item";
    private const string AbilityItemIconClassName = "ability-item__icon";
    private const string AbilityItemCooldownClassName = "ability-item--cooldown";
    private const string AbilityItemSelectedClassName = "ability-item--selected";

    private const string HoverCardElementName = "HoverCard";
    private const string HoverCardVisibleClassName = "hover-card--visible";
    private const string HoverCardEnemyClassName = "hover-card--enemy";
    private const string HoverCardFriendlyClassName = "hover-card--friendly";

    private const string VictoryStatusText = "Победа";
    private const string DefeatStatusText = "Поражение";
    private const string FleeStatusText = "Побег";

    public enum PanelName
    {
        TacticPanel,
        CombatPanel,
        ResultPanel
    }

    [SerializeField] private UIDocument _uiDocument;

    private readonly Dictionary<PanelName, VisualElement> _panels = new();
    private PanelName? _currentPanel;

    private Button _startCombatButton;
    private Button _leaveCombatButton;
    private Button _finishBattleButton;
    private Button _defendButton;
    private Button _skipTurnButton;
    private Label _resultStatusLabel;
    private VisualElement _queueContainer;
    private readonly List<VisualElement> _abilityListContainers = new();
    private readonly Dictionary<BattleAbilitySO, List<VisualElement>> _abilityElements = new();
    private BattleAbilityManager _currentAbilityManager;
    private IReadOnlySquadModel _currentAbilityOwner;
    private BattleAbilitySO _highlightedAbility;

    private VisualElement _hoverCard;
    private VisualElement _hoverCardIcon;
    private Label _hoverCardTitle;
    private readonly List<Label> _hoverCardInfoLabels = new();
    private BattleSquadController _currentHoveredSquad;
    private IReadOnlySquadModel _currentHoveredModel;
    private bool _isHoverCardDirty;
    private Camera _mainCamera;

    private bool _isAttached;

    public event Action OnStartCombat;
    public event Action OnLeaveCombat;
    public event Action OnFinishBattle;
    public event Action OnDefend;
    public event Action OnSkipTurn;
    public event Action<BattleAbilitySO> OnSelectAbility;

    private void Awake()
    {
        TryRegisterLifecycleCallbacks();
    }

    private void OnEnable()
    {
        TryRegisterLifecycleCallbacks();
    }

    private void OnDestroy()
    {
        DetachFromPanel();

        if (_uiDocument?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }
    }

    private void Update()
    {
        if (!_isAttached)
            return;

        UpdateHoverCard();
    }

    public void AttachToPanel(UIDocument document)
    {
        if (document == null)
        {
            Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(AttachToPanel)}] UIDocument reference is missing.");
            return;
        }

        if (_isAttached)
        {
            DetachFromPanel();
        }

        _uiDocument = document;

        VisualElement root = document.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(AttachToPanel)}] UIDocument root element is missing.");
            return;
        }

        VisualElement body = root.Q(BodyElementName);
        if (body == null)
        {
            Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(AttachToPanel)}] Body element was not found in the UI document.");
            return;
        }

        PanelName? previousPanel = _currentPanel;

        _panels.Clear();
        foreach (PanelName panelName in Enum.GetValues(typeof(PanelName)))
        {
            VisualElement panel = body.Q<VisualElement>(panelName.ToString());
            if (panel == null)
            {
                Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(AttachToPanel)}] Panel '{panelName}' was not found.");
                continue;
            }

            _panels[panelName] = panel;
        }

        _startCombatButton = body.Q<Button>(StartCombatButtonName);
        _leaveCombatButton = body.Q<Button>(LeaveCombatButtonName);
        _finishBattleButton = body.Q<Button>(FinishBattleButtonName);
        _defendButton = body.Q<Button>("DefendButton");
        _skipTurnButton = body.Q<Button>("SkipTurnButton");
        _resultStatusLabel = body.Q<Label>(ResultStatusLabelName);

        _queueContainer = body.Q<VisualElement>(QueueListElementName);
        if (_queueContainer != null)
        {
            _queueContainer.Clear();
            _queueContainer.style.display = DisplayStyle.None;
        }

        _abilityListContainers.Clear();
        foreach (VisualElement container in body.Query<VisualElement>(AbilityListElementName).ToList())
        {
            if (container == null)
                continue;

            container.Clear();
            container.style.display = DisplayStyle.None;
            _abilityListContainers.Add(container);
        }

        _abilityElements.Clear();
        _currentAbilityManager = null;
        _currentAbilityOwner = null;
        _highlightedAbility = null;
        _hoverCard = body.Q<VisualElement>(HoverCardElementName);
        InitializeHoverCardElements();
        HideHoverCard();
        _isHoverCardDirty = false;
        _mainCamera = Camera.main;
        _currentPanel = null;

        _startCombatButton?.RegisterCallback<ClickEvent>(HandleStartCombatClicked);
        _leaveCombatButton?.RegisterCallback<ClickEvent>(HandleLeaveCombatClicked);
        _finishBattleButton?.RegisterCallback<ClickEvent>(HandleFinishBattleClicked);
        _defendButton?.RegisterCallback<ClickEvent>(HandleDefendClicked);
        _skipTurnButton?.RegisterCallback<ClickEvent>(HandleSkipTurnClicked);

        _isAttached = true;

        if (previousPanel.HasValue && _panels.ContainsKey(previousPanel.Value))
        {
            ShowPanel(previousPanel.Value);
        }
    }

    public void DetachFromPanel()
    {
        if (!_isAttached)
        {
            return;
        }

        _startCombatButton?.UnregisterCallback<ClickEvent>(HandleStartCombatClicked);
        _leaveCombatButton?.UnregisterCallback<ClickEvent>(HandleLeaveCombatClicked);
        _finishBattleButton?.UnregisterCallback<ClickEvent>(HandleFinishBattleClicked);
        _defendButton?.UnregisterCallback<ClickEvent>(HandleDefendClicked);
        _skipTurnButton?.UnregisterCallback<ClickEvent>(HandleSkipTurnClicked);

        _startCombatButton = null;
        _leaveCombatButton = null;
        _finishBattleButton = null;
        _defendButton = null;
        _skipTurnButton = null;

        if (_queueContainer != null)
        {
            _queueContainer.Clear();
            _queueContainer.style.display = DisplayStyle.None;
            _queueContainer = null;
        }

        ClearAbilityList();
        foreach (VisualElement container in _abilityListContainers)
        {
            if (container == null)
                continue;

            container.Clear();
            container.style.display = DisplayStyle.None;
        }

        _abilityListContainers.Clear();
        _abilityElements.Clear();
        _currentAbilityManager = null;
        _currentAbilityOwner = null;
        _highlightedAbility = null;

        HideHoverCard();
        _hoverCardInfoLabels.Clear();
        _hoverCardIcon = null;
        _hoverCardTitle = null;
        _hoverCard = null;
        _mainCamera = null;

        _resultStatusLabel = null;
        _panels.Clear();

        _isAttached = false;
    }

    public void ShowPanel(PanelName panelName)
    {
        if (_panels.Count == 0)
            return;

        if (!_panels.TryGetValue(panelName, out VisualElement targetPanel))
        {
            Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(ShowPanel)}] Panel '{panelName}' is not registered.");
            return;
        }

        foreach (KeyValuePair<PanelName, VisualElement> pair in _panels)
        {
            bool isTarget = pair.Key == panelName;
            pair.Value.style.display = isTarget ? DisplayStyle.Flex : DisplayStyle.None;
            pair.Value.style.visibility = isTarget ? Visibility.Visible : Visibility.Hidden;
        }

        _currentPanel = panelName;
    }

    public void RenderQueue(BattleQueueController battleQueueController)
    {
        if (_queueContainer == null)
            return;

        _queueContainer.Clear();

        if (battleQueueController == null)
        {
            _queueContainer.style.display = DisplayStyle.None;
            return;
        }

        IReadOnlyList<IReadOnlySquadModel> queue = battleQueueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            _queueContainer.style.display = DisplayStyle.None;
            return;
        }

        foreach (IReadOnlySquadModel unit in queue)
        {
            if (unit == null)
                continue;

            VisualElement queueItem = CreateQueueItem(unit);
            _queueContainer.Add(queueItem);
        }

        _queueContainer.style.display = _queueContainer.childCount > 0
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    public void RenderAbilityList(BattleAbilitySO[] abilities, BattleAbilityManager abilityManager, IReadOnlySquadModel owner)
    {
        ClearAbilityList();

        _currentAbilityManager = abilityManager;
        _currentAbilityOwner = owner;

        if (abilities == null || abilities.Length == 0 || _abilityListContainers.Count == 0)
        {
            UpdateAbilityListVisibility(false);
            return;
        }

        foreach (BattleAbilitySO ability in abilities)
        {
            if (ability == null)
                continue;

            foreach (VisualElement container in _abilityListContainers)
            {
                if (container == null)
                    continue;

                VisualElement abilityElement = CreateAbilityElement(ability);
                container.Add(abilityElement);
                RegisterAbilityElement(ability, abilityElement);
            }
        }

        UpdateAbilityListVisibility(_abilityElements.Count > 0);
        RefreshAbilityAvailability();
    }

    public void RefreshAbilityAvailability()
    {
        if (_abilityElements.Count == 0)
            return;

        foreach (KeyValuePair<BattleAbilitySO, List<VisualElement>> pair in _abilityElements)
        {
            bool isReady = IsAbilityReady(pair.Key);

            foreach (VisualElement element in pair.Value)
            {
                if (element == null)
                    continue;

                if (isReady)
                {
                    element.RemoveFromClassList(AbilityItemCooldownClassName);
                    element.SetEnabled(true);
                }
                else
                {
                    element.AddToClassList(AbilityItemCooldownClassName);
                    element.SetEnabled(false);
                }
            }
        }
    }

    public void HighlightAbility(BattleAbilitySO ability)
    {
        _highlightedAbility = ability;

        foreach (KeyValuePair<BattleAbilitySO, List<VisualElement>> pair in _abilityElements)
        {
            bool shouldHighlight = ability != null && ReferenceEquals(pair.Key, ability);

            foreach (VisualElement element in pair.Value)
            {
                if (element == null)
                    continue;

                if (shouldHighlight)
                    element.AddToClassList(AbilityItemSelectedClassName);
                else
                    element.RemoveFromClassList(AbilityItemSelectedClassName);
            }
        }
    }

    public void ResetAbilityHighlight()
    {
        _highlightedAbility = null;

        foreach (KeyValuePair<BattleAbilitySO, List<VisualElement>> pair in _abilityElements)
        {
            foreach (VisualElement element in pair.Value)
            {
                element?.RemoveFromClassList(AbilityItemSelectedClassName);
            }
        }
    }

    public void SetDefendButtonInteractable(bool interactable)
    {
        if (_defendButton == null)
            return;

        _defendButton.SetEnabled(interactable);
    }

    public void ShowResult(BattleResult result)
    {
        if (_resultStatusLabel == null)
            return;

        _resultStatusLabel.text = result.Status switch
        {
            BattleResultStatus.Victory => VictoryStatusText,
            BattleResultStatus.Defeat => DefeatStatusText,
            BattleResultStatus.Flee => FleeStatusText,
            _ => string.Empty
        };
    }

    private void HandleStartCombatClicked(ClickEvent evt)
    {
        OnStartCombat?.Invoke();
    }

    private void HandleLeaveCombatClicked(ClickEvent evt)
    {
        OnLeaveCombat?.Invoke();
    }

    private void HandleFinishBattleClicked(ClickEvent evt)
    {
        OnFinishBattle?.Invoke();
    }

    private void HandleDefendClicked(ClickEvent evt)
    {
        OnDefend?.Invoke();
    }

    private void HandleSkipTurnClicked(ClickEvent evt)
    {
        OnSkipTurn?.Invoke();
    }

    private void UpdateHoverCard()
    {
        if (_hoverCard == null)
            return;

        BattleSquadController squad = FindSquadUnderPointer();
        if (squad == null)
        {
            HideHoverCard();
            return;
        }

        IReadOnlySquadModel model = squad.GetSquadModel();
        if (model == null)
        {
            HideHoverCard();
            return;
        }

        if (!ReferenceEquals(_currentHoveredSquad, squad) || !ReferenceEquals(_currentHoveredModel, model))
        {
            UnsubscribeFromHoveredModel();
            _currentHoveredSquad = squad;
            _currentHoveredModel = model;
            SubscribeToHoveredModel(model);
            _isHoverCardDirty = true;
        }

        if (_isHoverCardDirty)
        {
            RenderHoverCard(model);
            _isHoverCardDirty = false;
        }

        ApplyHoverCardOrientation(model);
        ShowHoverCard();
    }

    private void InitializeHoverCardElements()
    {
        _hoverCardIcon = null;
        _hoverCardTitle = null;
        _hoverCardInfoLabels.Clear();

        if (_hoverCard == null)
            return;

        _hoverCardIcon = _hoverCard.Q<VisualElement>("Icon");
        _hoverCardTitle = _hoverCard.Q<Label>("Title");

        VisualElement infoContainer = _hoverCard.Q<VisualElement>("Info");
        infoContainer?.Query<Label>().ForEach(label =>
        {
            if (label == null)
                return;

            label.text = string.Empty;
            label.style.display = DisplayStyle.None;
            _hoverCardInfoLabels.Add(label);
        });

        if (_hoverCardIcon != null)
        {
            _hoverCardIcon.style.backgroundImage = StyleKeyword.Null;
            _hoverCardIcon.tooltip = string.Empty;
        }

        if (_hoverCardTitle != null)
            _hoverCardTitle.text = string.Empty;
    }

    private void RenderHoverCard(IReadOnlySquadModel model)
    {
        if (model == null)
            return;

        if (_hoverCardIcon != null)
        {
            if (model.Icon != null)
                _hoverCardIcon.style.backgroundImage = new StyleBackground(model.Icon);
            else
                _hoverCardIcon.style.backgroundImage = StyleKeyword.Null;

            _hoverCardIcon.tooltip = model.UnitName ?? string.Empty;
        }

        if (_hoverCardTitle != null)
            _hoverCardTitle.text = model.UnitName ?? string.Empty;

        IReadOnlyList<string> stats = BuildSquadStatEntries(model);
        int statsCount = stats.Count;

        for (int i = 0; i < _hoverCardInfoLabels.Count; i++)
        {
            Label label = _hoverCardInfoLabels[i];
            if (label == null)
                continue;

            if (i < statsCount)
            {
                label.text = stats[i];
                label.style.display = DisplayStyle.Flex;
            }
            else
            {
                label.text = string.Empty;
                label.style.display = DisplayStyle.None;
            }
        }
    }

    private void ApplyHoverCardOrientation(IReadOnlySquadModel model)
    {
        if (_hoverCard == null || model == null)
            return;

        _hoverCard.RemoveFromClassList(HoverCardEnemyClassName);
        _hoverCard.RemoveFromClassList(HoverCardFriendlyClassName);

        bool isEnemy = model.IsEnemy();
        string orientationClass = isEnemy ? HoverCardEnemyClassName : HoverCardFriendlyClassName;
        _hoverCard.AddToClassList(orientationClass);
    }

    private void ShowHoverCard()
    {
        if (_hoverCard == null)
            return;

        if (!_hoverCard.ClassListContains(HoverCardVisibleClassName))
            _hoverCard.AddToClassList(HoverCardVisibleClassName);
    }

    private void HideHoverCard()
    {
        UnsubscribeFromHoveredModel();
        _currentHoveredSquad = null;
        _isHoverCardDirty = false;

        if (_hoverCard == null)
            return;

        _hoverCard.RemoveFromClassList(HoverCardVisibleClassName);
        _hoverCard.RemoveFromClassList(HoverCardEnemyClassName);
        _hoverCard.RemoveFromClassList(HoverCardFriendlyClassName);
    }

    private void SubscribeToHoveredModel(IReadOnlySquadModel model)
    {
        if (model == null)
            return;

        model.Changed -= HandleHoveredModelChanged;
        model.Changed += HandleHoveredModelChanged;
    }

    private void UnsubscribeFromHoveredModel()
    {
        if (_currentHoveredModel == null)
            return;

        _currentHoveredModel.Changed -= HandleHoveredModelChanged;
        _currentHoveredModel = null;
    }

    private void HandleHoveredModelChanged(IReadOnlySquadModel _)
    {
        _isHoverCardDirty = true;
    }

    private BattleSquadController FindSquadUnderPointer()
    {
        if (!TryGetPointerScreenPosition(out Vector2 pointerPosition))
            return null;

        Camera camera = _mainCamera != null ? _mainCamera : Camera.main;
        if (camera == null)
            return null;

        _mainCamera = camera;

        Ray ray = camera.ScreenPointToRay(new Vector3(pointerPosition.x, pointerPosition.y, 0f));
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Transform hitTransform = hitInfo.transform;
            if (hitTransform != null)
            {
                BattleSquadController controller = hitTransform.GetComponentInParent<BattleSquadController>();
                if (controller != null)
                    return controller;
            }
        }

        Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, Mathf.Abs(camera.transform.position.z)));
        Collider2D hit2D = Physics2D.OverlapPoint(worldPoint);
        if (hit2D != null)
        {
            Transform hitTransform2D = hit2D.transform;
            if (hitTransform2D != null)
            {
                BattleSquadController controller2D = hitTransform2D.GetComponentInParent<BattleSquadController>();
                if (controller2D != null)
                    return controller2D;
            }
        }

        return null;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 position)
    {
        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        if (Pointer.current != null)
        {
            position = Pointer.current.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    private IReadOnlyList<string> BuildSquadStatEntries(IReadOnlySquadModel model)
    {
        if (model == null)
            return Array.Empty<string>();

        List<string> entries = new()
        {
            $"Название: {model.UnitName}",
            $"Количество: {model.Count}",
            $"Здоровье: {FormatValue(model.Health)}",
            $"Физическая защита: {FormatPercent(model.PhysicalDefense)}",
            $"Магическая защита: {FormatPercent(model.MagicDefense)}",
            $"Абсолютная защита: {FormatPercent(model.AbsoluteDefense)}",
            $"Тип атаки: {FormatAttackKind(model.AttackKind)}",
            $"Тип урона: {FormatDamageType(model.DamageType)}",
        };

        (float minDamage, float maxDamage) = model.GetBaseDamageRange();
        entries.Add($"Урон: {FormatValue(minDamage)} - {FormatValue(maxDamage)}");
        entries.Add($"Скорость: {FormatValue(model.Speed)}");
        entries.Add($"Инициатива: {FormatValue(model.Initiative)}");
        entries.Add($"Шанс критического удара: {FormatPercent(model.CritChance)}");
        entries.Add($"Критический множитель: {FormatValue(model.CritMultiplier)}");
        entries.Add($"Шанс промаха: {FormatPercent(model.MissChance)}");

        return entries;
    }

    private static string FormatValue(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatPercent(float value)
    {
        return value.ToString("P0", CultureInfo.InvariantCulture);
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

    private VisualElement CreateQueueItem(IReadOnlySquadModel unit)
    {
        var item = new VisualElement();
        item.AddToClassList(QueueItemClassName);

        var iconElement = new VisualElement();
        iconElement.AddToClassList(QueueItemIconClassName);
        if (unit.Icon != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(unit.Icon);
        }

        var countLabel = new Label(unit.Count.ToString());
        countLabel.AddToClassList(QueueItemCountClassName);

        item.Add(iconElement);
        item.Add(countLabel);

        return item;
    }

    private VisualElement CreateAbilityElement(BattleAbilitySO ability)
    {
        var element = new VisualElement();
        element.AddToClassList(AbilityItemClassName);
        element.tooltip = FormatAbilityTooltip(ability);

        var iconElement = new VisualElement();
        iconElement.AddToClassList(AbilityItemIconClassName);
        if (ability.Icon != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(ability.Icon);
        }

        element.Add(iconElement);

        element.RegisterCallback<ClickEvent>(_ => HandleAbilityClick(ability));

        return element;
    }

    private void RegisterAbilityElement(BattleAbilitySO ability, VisualElement element)
    {
        if (!_abilityElements.TryGetValue(ability, out List<VisualElement> elements))
        {
            elements = new List<VisualElement>();
            _abilityElements[ability] = elements;
        }

        elements.Add(element);
    }

    private void ClearAbilityList()
    {
        foreach (VisualElement container in _abilityListContainers)
        {
            container?.Clear();
        }

        _abilityElements.Clear();
        _highlightedAbility = null;
    }

    private void UpdateAbilityListVisibility(bool isVisible)
    {
        DisplayStyle display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (VisualElement container in _abilityListContainers)
        {
            if (container == null)
                continue;

            container.style.display = display;
        }
    }

    private void TryRegisterLifecycleCallbacks()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            root.RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

            if (!_isAttached && root.panel != null)
                AttachToPanel(_uiDocument);
        }
    }

    private void HandleAttachToPanel(AttachToPanelEvent _)
    {
        if (!_isAttached)
            AttachToPanel(_uiDocument);
    }

    private void HandleDetachFromPanel(DetachFromPanelEvent _)
    {
        DetachFromPanel();
    }

    private bool IsAbilityReady(BattleAbilitySO ability)
    {
        if (ability == null)
            return false;

        if (_currentAbilityManager == null || _currentAbilityOwner == null)
            return true;

        return _currentAbilityManager.IsAbilityReady(_currentAbilityOwner, ability);
    }

    private void HandleAbilityClick(BattleAbilitySO ability)
    {
        if (ability == null)
            return;

        if (!IsAbilityReady(ability))
            return;

        OnSelectAbility?.Invoke(ability);
    }

    private string FormatAbilityTooltip(BattleAbilitySO ability)
    {
        if (ability == null)
            return string.Empty;

        string cooldownLabel = GetCooldownLabel(ability.Cooldown);
        return $"{ability.AbilityName}\n{ability.Description}\nПерезарядка: {ability.Cooldown} {cooldownLabel}";
    }

    private static string GetCooldownLabel(int cooldown)
    {
        int absoluteCooldown = Math.Abs(cooldown);
        int lastTwoDigits = absoluteCooldown % 100;
        int lastDigit = absoluteCooldown % 10;

        if (lastDigit == 1 && lastTwoDigits != 11)
            return "раунд";

        if (lastDigit >= 2 && lastDigit <= 4 && (lastTwoDigits < 12 || lastTwoDigits > 14))
            return "раунда";

        return "раундов";
    }
}
