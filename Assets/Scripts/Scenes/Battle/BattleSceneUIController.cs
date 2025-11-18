using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UICommon.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public sealed class BattleSceneUIController : MonoBehaviour, ISceneUIController
{
    private const string BodyElementName = "Body";
    private const string StartCombatButtonName = "StartCombatButton";
    private const string LeaveCombatButtonName = "LeaveCombatButton";
    private const string FinishBattleButtonName = "FinishBattleButton";
    private const string ResultStatusLabelName = "ResultStatusLabel";
    private const string PlayerSquadResultsElementName = "PlayerSquadResults";
    private const string EnemySquadResultsElementName = "EnemySquadResults";
    private const string QueueListElementName = "QueueList";
    private const string AbilityListElementName = "AbilityList";

    private const string QueueItemClassName = "queue-item";
    private const string QueueItemIconClassName = "queue-item__icon";
    private const string QueueItemCountClassName = "queue-item__count";

    private const string AbilityItemClassName = "ability-item";
    private const string AbilityItemIconClassName = "ability-item__icon";
    private const string AbilityItemCooldownClassName = "ability-item--cooldown";
    private const string AbilityItemSelectedClassName = "ability-item--selected";

    private const string SquadInfoCardElementName = "SquadInfoCard";
    private const string BattleCardHiddenClassName = "battle-card--hidden";
    private const string BattleCardVisibleClassName = "battle-card--visible";
    private const string BattleCardLeftClassName = "battle-card--left";
    private const string BattleCardRightClassName = "battle-card--right";
    private const string UnitsLayerName = "Units";

    private const string AbilityTooltipClassName = "ability-tooltip";
    private const string AbilityTooltipVisibleClassName = "ability-tooltip--visible";
    private const string AbilityTooltipTextClassName = "ability-tooltip__text";
    private const float AbilityTooltipOffset = 8f;

    private const string ResultSquadClassName = "result-squad";
    private const string ResultSquadDeadClassName = "result-squad--dead";
    private const string ResultSquadVisibleClassName = "result-squad--visible";
    private const string ResultSquadIconClassName = "result-squad__icon";
    private const string ResultSquadCountWrapperClassName = "result-squad__count-wrapper";
    private const string ResultSquadCountClassName = "result-squad__count";
    private const string ResultSquadDeltaClassName = "result-squad__delta";
    private const string ResultSquadDeltaVisibleClassName = "result-squad__delta--visible";
    private const string ResultSquadNameClassName = "result-squad__name";

    private const float ResultSquadAnimationDelaySeconds = 0.08f;

    private static readonly UnitCardStatField[] BattleCardStatFields =
    {
        UnitCardStatField.Count,
        UnitCardStatField.Health,
        UnitCardStatField.DamageRange,
        UnitCardStatField.Initiative,
        UnitCardStatField.PhysicalDefense,
        UnitCardStatField.MagicDefense,
        UnitCardStatField.AbsoluteDefense,
        UnitCardStatField.AttackKind,
        UnitCardStatField.DamageType,
        UnitCardStatField.CritChance,
        UnitCardStatField.CritMultiplier,
        UnitCardStatField.MissChance
    };

    private const string VictoryStatusText = "Победа";
    private const string DefeatStatusText = "Поражение";
    private const string FleeStatusText = "Побег";

    public enum PanelName
    {
        TacticPanel,
        CombatPanel,
        ResultPanel
    }

    private readonly struct ResultSquadView
    {
        public ResultSquadView(VisualElement root, VisualElement delta)
        {
            Root = root;
            Delta = delta;
        }

        public VisualElement Root { get; }

        public VisualElement Delta { get; }
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
    private VisualElement _playerSquadResults;
    private VisualElement _enemySquadResults;
    private VisualElement _queueContainer;
    private readonly List<VisualElement> _abilityListContainers = new();
    private readonly Dictionary<BattleAbilitySO, List<VisualElement>> _abilityElements = new();
    private BattleAbilityManager _currentAbilityManager;
    private IReadOnlySquadModel _currentAbilityOwner;
    private BattleAbilitySO _highlightedAbility;
    private VisualElement _abilityTooltip;
    private Label _abilityTooltipLabel;
    private VisualElement _abilityTooltipTarget;
    private VisualElement _squadInfoCard;
    private UnitCardWidget _squadInfoCardWidget;
    private IReadOnlySquadModel _displayedSquadModel;
    private BattleSquadEffectsController _displayedEffectsController;
    private Camera _mainCamera;
    private int _unitsLayerMask;
    private Sequence _resultSquadAnimationSequence;
    private VisualElement _bodyElement;

    private bool _isAttached;

    public event Action OnStartCombat;
    public event Action OnLeaveCombat;
    public event Action OnFinishBattle;
    public event Action OnDefend;
    public event Action OnSkipTurn;
    public event Action<BattleAbilitySO> OnSelectAbility;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _unitsLayerMask = LayerMask.GetMask(UnitsLayerName);
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
        UpdateSquadInfoCardFromPointer();
    }

    public void AttachToPanel(UIDocument document)
    {
        if (document == null)
        {
            Debug.LogWarning($"[{nameof(BattleSceneUIController)}.{nameof(AttachToPanel)}] UIDocument reference is missing.");
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
            Debug.LogWarning($"[{nameof(BattleSceneUIController)}.{nameof(AttachToPanel)}] UIDocument root element is missing.");
            return;
        }

        VisualElement body = root.Q(BodyElementName);
        if (body == null)
        {
            Debug.LogWarning($"[{nameof(BattleSceneUIController)}.{nameof(AttachToPanel)}] Body element was not found in the UI document.");
            return;
        }

        _bodyElement = body;

        PanelName? previousPanel = _currentPanel;

        _panels.Clear();
        foreach (PanelName panelName in Enum.GetValues(typeof(PanelName)))
        {
            VisualElement panel = body.Q<VisualElement>(panelName.ToString());
            if (panel == null)
            {
                Debug.LogWarning($"[{nameof(BattleSceneUIController)}.{nameof(AttachToPanel)}] Panel '{panelName}' was not found.");
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
        _playerSquadResults = body.Q<VisualElement>(PlayerSquadResultsElementName);
        _enemySquadResults = body.Q<VisualElement>(EnemySquadResultsElementName);

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
        _currentPanel = null;

        InitializeSquadInfoCard(body);
        InitializeAbilityTooltip(body);

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

        StopResultSquadAnimation();

        HideSquadInfoCard();
        _squadInfoCard = null;
        _squadInfoCardWidget = null;
        _displayedEffectsController = null;

        _resultStatusLabel = null;
        _playerSquadResults?.Clear();
        _enemySquadResults?.Clear();
        _playerSquadResults = null;
        _enemySquadResults = null;
        _panels.Clear();

        HideAbilityTooltip();
        _abilityTooltip?.RemoveFromHierarchy();
        _abilityTooltip = null;
        _abilityTooltipLabel = null;
        _abilityTooltipTarget = null;
        _bodyElement = null;

        _isAttached = false;
    }

    public void ShowPanel(PanelName panelName)
    {
        if (_panels.Count == 0)
            return;

        if (!_panels.TryGetValue(panelName, out VisualElement targetPanel))
        {
            Debug.LogWarning($"[{nameof(BattleSceneUIController)}.{nameof(ShowPanel)}] Panel '{panelName}' is not registered.");
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
        if (result == null)
            return;

        StopResultSquadAnimation();

        if (_resultStatusLabel != null)
        {
            _resultStatusLabel.text = result.Status switch
            {
                BattleResultStatus.Victory => VictoryStatusText,
                BattleResultStatus.Defeat => DefeatStatusText,
                BattleResultStatus.Flee => FleeStatusText,
                _ => string.Empty
            };
        }

        List<ResultSquadView> friendlyElements = RenderResultSquads(
            _playerSquadResults,
            result.BattleUnitsResult.FriendlyUnits,
            result);

        List<ResultSquadView> enemyElements = RenderResultSquads(
            _enemySquadResults,
            result.BattleUnitsResult.EnemyUnits,
            result);

        AnimateResultSquadReveal(friendlyElements, enemyElements);
    }

    private List<ResultSquadView> RenderResultSquads(
        VisualElement container,
        IReadOnlyList<IReadOnlySquadModel> squads,
        BattleResult battleResult)
    {
        var createdElements = new List<ResultSquadView>();

        if (container == null)
            return createdElements;

        container.Clear();

        if (squads == null || squads.Count == 0)
            return createdElements;

        foreach (IReadOnlySquadModel squad in squads)
        {
            if (squad == null)
                continue;

            ResultSquadView squadView = CreateResultSquadElement(squad, battleResult);
            container.Add(squadView.Root);
            createdElements.Add(squadView);
        }

        return createdElements;
    }

    private ResultSquadView CreateResultSquadElement(IReadOnlySquadModel squad, BattleResult battleResult)
    {
        var root = new VisualElement();
        root.AddToClassList(ResultSquadClassName);

        var icon = new VisualElement();
        icon.AddToClassList(ResultSquadIconClassName);
        if (squad.Icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(squad.Icon);
        }

        var countWrapper = new VisualElement();
        countWrapper.AddToClassList(ResultSquadCountWrapperClassName);

        var countLabel = new Label(Mathf.Max(0, squad.Count).ToString());
        countLabel.AddToClassList(ResultSquadCountClassName);
        countWrapper.Add(countLabel);

        Label deltaLabel = CreateResultSquadDeltaLabel(battleResult, squad);
        if (deltaLabel != null)
        {
            countWrapper.Add(deltaLabel);
        }

        var nameLabel = new Label(squad.UnitName ?? string.Empty);
        nameLabel.AddToClassList(ResultSquadNameClassName);

        root.Add(icon);
        root.Add(countWrapper);
        root.Add(nameLabel);

        if (squad.IsEmpty || squad.Count <= 0)
        {
            root.AddToClassList(ResultSquadDeadClassName);
        }

        return new ResultSquadView(root, deltaLabel);
    }

    private Label CreateResultSquadDeltaLabel(BattleResult battleResult, IReadOnlySquadModel squad)
    {
        if (battleResult == null || squad == null)
            return null;

        int finalCount = Mathf.Max(0, squad.Count);
        int initialCount = Mathf.Max(0, battleResult.GetInitialCount(squad));
        int delta = finalCount - initialCount;

        if (delta == 0)
            return null;

        string text = FormatResultSquadDelta(delta);
        if (string.IsNullOrEmpty(text))
            return null;

        var label = new Label(text);
        label.AddToClassList(ResultSquadDeltaClassName);
        return label;
    }

    private static string FormatResultSquadDelta(int delta)
    {
        if (delta == 0)
            return null;

        string sign = delta > 0 ? "+" : "-";
        return $"({sign}{Mathf.Abs(delta)})";
    }

    private void AnimateResultSquadReveal(
        IReadOnlyList<ResultSquadView> friendlySquads,
        IReadOnlyList<ResultSquadView> enemySquads)
    {
        StopResultSquadAnimation();

        bool hasFriendly = friendlySquads != null && friendlySquads.Count > 0;
        bool hasEnemy = enemySquads != null && enemySquads.Count > 0;

        if (!hasFriendly && !hasEnemy)
            return;

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        AppendSquadGroupReveal(sequence, friendlySquads);
        AppendSquadGroupReveal(sequence, enemySquads);

        List<VisualElement> deltaElements = CollectDeltaElements(friendlySquads, enemySquads);
        if (deltaElements.Count > 0)
        {
            sequence.AppendInterval(ResultSquadAnimationDelaySeconds);
            AppendDeltaReveal(sequence, deltaElements);
        }

        _resultSquadAnimationSequence = sequence;
    }

    private static List<VisualElement> CollectDeltaElements(
        IReadOnlyList<ResultSquadView> friendlySquads,
        IReadOnlyList<ResultSquadView> enemySquads)
    {
        var deltaElements = new List<VisualElement>();
        AppendDeltaElements(deltaElements, friendlySquads);
        AppendDeltaElements(deltaElements, enemySquads);
        return deltaElements;
    }

    private static void AppendDeltaElements(List<VisualElement> target, IReadOnlyList<ResultSquadView> source)
    {
        if (target == null || source == null || source.Count == 0)
            return;

        foreach (ResultSquadView squad in source)
        {
            if (squad.Delta != null)
            {
                target.Add(squad.Delta);
            }
        }
    }

    private void AppendSquadGroupReveal(Sequence sequence, IReadOnlyList<ResultSquadView> squads)
    {
        if (sequence == null || squads == null || squads.Count == 0)
            return;

        foreach (ResultSquadView squad in squads)
        {
            VisualElement element = squad.Root;
            if (element == null)
                continue;

            sequence.AppendCallback(() => element.AddToClassList(ResultSquadVisibleClassName));
            sequence.AppendInterval(ResultSquadAnimationDelaySeconds);
        }
    }

    private void AppendDeltaReveal(Sequence sequence, IReadOnlyList<VisualElement> deltas)
    {
        if (sequence == null || deltas == null || deltas.Count == 0)
            return;

        foreach (VisualElement delta in deltas)
        {
            if (delta == null)
                continue;

            sequence.AppendCallback(() => delta.AddToClassList(ResultSquadDeltaVisibleClassName));
            sequence.AppendInterval(ResultSquadAnimationDelaySeconds);
        }
    }

    private void StopResultSquadAnimation()
    {
        if (_resultSquadAnimationSequence == null)
            return;

        _resultSquadAnimationSequence.Kill();
        _resultSquadAnimationSequence = null;
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

        var iconElement = new VisualElement();
        iconElement.AddToClassList(AbilityItemIconClassName);
        if (ability.Icon != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(ability.Icon);
        }

        element.Add(iconElement);

        element.RegisterCallback<ClickEvent>(_ => HandleAbilityClick(ability));
        element.RegisterCallback<PointerEnterEvent>(_ => ShowAbilityTooltip(ability, element));
        element.RegisterCallback<PointerLeaveEvent>(_ => HideAbilityTooltip());
        element.RegisterCallback<PointerMoveEvent>(_ => UpdateAbilityTooltipFromTarget(element));

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
        HideAbilityTooltip();
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

    private void InitializeAbilityTooltip(VisualElement body)
    {
        if (body == null)
            return;

        _abilityTooltip?.RemoveFromHierarchy();

        _abilityTooltip = new VisualElement
        {
            name = "AbilityTooltip"
        };
        _abilityTooltip.AddToClassList(AbilityTooltipClassName);

        _abilityTooltipLabel = new Label();
        _abilityTooltipLabel.AddToClassList(AbilityTooltipTextClassName);
        _abilityTooltipLabel.enableRichText = true;
        _abilityTooltip.Add(_abilityTooltipLabel);

        body.Add(_abilityTooltip);

        HideAbilityTooltip();
    }

    private void ShowAbilityTooltip(BattleAbilitySO ability, VisualElement target)
    {
        if (ability == null || target == null || _abilityTooltip == null || _abilityTooltipLabel == null)
            return;

        _abilityTooltipTarget = target;
        _abilityTooltipLabel.text = ResolveAbilityTooltipText(ability);
        _abilityTooltip.AddToClassList(AbilityTooltipVisibleClassName);
        UpdateAbilityTooltipPosition(target.worldBound);

        _abilityTooltip.schedule.Execute(() =>
        {
            if (_abilityTooltipTarget == target)
                UpdateAbilityTooltipPosition(target.worldBound);
        });
    }

    private void UpdateAbilityTooltipFromTarget(VisualElement target)
    {
        if (target == null || !ReferenceEquals(_abilityTooltipTarget, target))
            return;

        UpdateAbilityTooltipPosition(target.worldBound);
    }

    private void UpdateAbilityTooltipPosition(Rect targetWorldBounds)
    {
        if (_abilityTooltip == null || _bodyElement == null)
            return;

        Vector2 topCenter = new(targetWorldBounds.xMin + targetWorldBounds.width * 0.5f, targetWorldBounds.yMin);
        Vector2 localPosition = _bodyElement.WorldToLocal(topCenter);

        float tooltipWidth = _abilityTooltip.resolvedStyle.width;
        float tooltipHeight = _abilityTooltip.resolvedStyle.height;

        float x = localPosition.x - tooltipWidth * 0.5f;
        float y = localPosition.y - tooltipHeight - AbilityTooltipOffset;

        float maxX = Mathf.Max(0f, _bodyElement.resolvedStyle.width - tooltipWidth);
        x = Mathf.Clamp(x, 0f, maxX);
        y = Mathf.Max(0f, y);

        _abilityTooltip.style.left = x;
        _abilityTooltip.style.top = y;
    }

    private void HideAbilityTooltip()
    {
        if (_abilityTooltip == null)
            return;

        _abilityTooltip.RemoveFromClassList(AbilityTooltipVisibleClassName);
        _abilityTooltipTarget = null;
    }

    private static string ResolveAbilityTooltipText(BattleAbilitySO ability)
    {
        if (ability == null)
            return string.Empty;

        string formatted = ability.GetFormatedDescription();
        return string.IsNullOrWhiteSpace(formatted) ? string.Empty : formatted;
    }

    private void InitializeSquadInfoCard(VisualElement body)
    {
        _squadInfoCard = body?.Q<VisualElement>(SquadInfoCardElementName);
        _squadInfoCardWidget = _squadInfoCard != null ? new UnitCardWidget(_squadInfoCard) : null;

        HideSquadInfoCard();
    }

    private void UpdateSquadInfoCardFromPointer()
    {
        if (!_isAttached || _squadInfoCard == null)
            return;

        BattleSquadController squadController = FindSquadUnderPointer();
        if (squadController == null)
        {
            if (_displayedSquadModel != null)
                HideSquadInfoCard();
            return;
        }

        ShowSquadInfoCard(squadController);
    }

    private void ShowSquadInfoCard(BattleSquadController controller)
    {
        if (controller == null || _squadInfoCard == null)
        {
            HideSquadInfoCard();
            return;
        }

        IReadOnlySquadModel squad = controller.GetSquadModel();
        if (squad == null)
        {
            HideSquadInfoCard();
            return;
        }

        BattleSquadEffectsController effectsController = controller.GetComponent<BattleSquadEffectsController>();

        if (!ReferenceEquals(_displayedSquadModel, squad))
        {
            UnsubscribeFromDisplayedSquad();
            _displayedSquadModel = squad;
            _displayedSquadModel.Changed += HandleDisplayedSquadChanged;
        }

        _displayedEffectsController = effectsController;

        UpdateSquadInfoContent(squad, effectsController.Effects);
        UpdateSquadCardPosition(squad);

        _squadInfoCard.EnableInClassList(BattleCardHiddenClassName, false);
        _squadInfoCard.EnableInClassList(BattleCardVisibleClassName, true);
    }

    private void HideSquadInfoCard()
    {
        UnsubscribeFromDisplayedSquad();
        _displayedEffectsController = null;

        if (_squadInfoCard == null)
            return;

        _squadInfoCard.EnableInClassList(BattleCardVisibleClassName, false);
        _squadInfoCard.EnableInClassList(BattleCardHiddenClassName, true);
    }

    private void HandleDisplayedSquadChanged(IReadOnlySquadModel squad)
    {
        if (squad == null || !ReferenceEquals(_displayedSquadModel, squad))
            return;

        UpdateSquadInfoContent(squad, _displayedEffectsController?.Effects);
    }

    private void UpdateSquadInfoContent(IReadOnlySquadModel squad, IReadOnlyList<BattleEffectSO> effects)
    {
        if (squad == null)
            return;

        IReadOnlyList<BattleAbilitySO> abilities = squad.Abilities ?? Array.Empty<BattleAbilitySO>();
        IReadOnlyList<BattleEffectSO> effectList = effects ?? Array.Empty<BattleEffectSO>();
        UnitCardRenderData data = new(squad, BattleCardStatFields, abilities, effectList, squad.UnitName);
        _squadInfoCardWidget?.Render(data);
    }

    private void UpdateSquadCardPosition(IReadOnlySquadModel squad)
    {
        if (_squadInfoCard == null)
            return;

        bool isFriendly = squad.IsFriendly() || squad.IsAlly() || squad.IsHero();
        _squadInfoCard.EnableInClassList(BattleCardLeftClassName, isFriendly);
        _squadInfoCard.EnableInClassList(BattleCardRightClassName, !isFriendly);
    }

    private void UnsubscribeFromDisplayedSquad()
    {
        if (_displayedSquadModel == null)
            return;

        _displayedSquadModel.Changed -= HandleDisplayedSquadChanged;
        _displayedSquadModel = null;
    }

    private BattleSquadController FindSquadUnderPointer()
    {
        // Получаем экранную позицию указателя
        if (!InputUtils.TryGetPointerScreenPosition(out Vector2 screenPos))
            return null;

        Camera cam = Camera.main;
        if (cam == null)
            return null;

        // Конвертация экран → мир
        // Для 2D правильный вариант: задаём Z = расстояние до плоскости мира
        // Если твои отряды находятся в Z = 0, то nearClipPlane подходит идеально
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(
            screenPos.x,
            screenPos.y,
            cam.nearClipPlane
        ));

        // Только слой Units
        int mask = LayerMask.GetMask("Units");

        // Точечный Raycast (как OverlapPoint, но с LayerMask)
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, 0f, mask);

        if (hit.collider != null)
        {
            return hit.collider.GetComponentInParent<BattleSquadController>();
        }

        return null;
    }
}
