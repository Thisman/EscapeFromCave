using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BattleUIController : MonoBehaviour
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

    public event Action OnStartCombat;
    public event Action OnLeaveCombat;
    public event Action OnFinishBattle;
    public event Action OnDefend;
    public event Action OnSkipTurn;
    public event Action<BattleAbilitySO> OnSelectAbility;

    private void Awake()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument == null)
        {
            Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(Awake)}] UIDocument reference is missing.");
            return;
        }

        VisualElement root = _uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(Awake)}] UIDocument root element is missing.");
            return;
        }

        VisualElement body = root.Q(BodyElementName);
        if (body == null)
        {
            Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(Awake)}] Body element was not found in the UI document.");
            return;
        }

        foreach (PanelName panelName in Enum.GetValues(typeof(PanelName)))
        {
            VisualElement panel = body.Q<VisualElement>(panelName.ToString());
            if (panel == null)
            {
                Debug.LogWarning($"[{nameof(BattleUIController)}.{nameof(Awake)}] Panel '{panelName}' was not found.");
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
            _queueContainer.style.display = DisplayStyle.None;
        }

        _abilityListContainers.Clear();
        foreach (VisualElement container in body.Query<VisualElement>(AbilityListElementName).ToList())
        {
            if (container == null)
                continue;

            container.style.display = DisplayStyle.None;
            _abilityListContainers.Add(container);
        }
    }

    private void OnEnable()
    {
        _startCombatButton?.RegisterCallback<ClickEvent>(HandleStartCombatClicked);
        _leaveCombatButton?.RegisterCallback<ClickEvent>(HandleLeaveCombatClicked);
        _finishBattleButton?.RegisterCallback<ClickEvent>(HandleFinishBattleClicked);
        _defendButton?.RegisterCallback<ClickEvent>(HandleDefendClicked);
        _skipTurnButton?.RegisterCallback<ClickEvent>(HandleSkipTurnClicked);
    }

    private void OnDisable()
    {
        _startCombatButton?.UnregisterCallback<ClickEvent>(HandleStartCombatClicked);
        _leaveCombatButton?.UnregisterCallback<ClickEvent>(HandleLeaveCombatClicked);
        _finishBattleButton?.UnregisterCallback<ClickEvent>(HandleFinishBattleClicked);
        _defendButton?.UnregisterCallback<ClickEvent>(HandleDefendClicked);
        _skipTurnButton?.UnregisterCallback<ClickEvent>(HandleSkipTurnClicked);
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
