using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UICommon.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

public enum BattleSceneElements
{
    Root,
    Body,
    StartCombatButton,
    LeaveCombatButton,
    FinishBattleButton,
    DefendButton,
    SkipTurnButton,
    ResultStatusLabel,
    PlayerSquadResults,
    EnemySquadResults,
    QueueList,
    SquadInfoCard,
    AbilityTooltip,
    AbilityTooltipLabel
}

public static class BattleSceneClassNames
{
    public const string QueueItem = "queue-item";
    public const string QueueItemIcon = "queue-item__icon";
    public const string QueueItemCount = "queue-item__count";

    public const string AbilityItem = "ability-item";
    public const string AbilityItemIcon = "ability-item__icon";
    public const string AbilityItemCooldownLabel = "ability-item__cooldown";
    public const string AbilityItemCooldown = "ability-item--cooldown";
    public const string AbilityItemSelected = "ability-item--selected";

    public const string BattleCardLeft = "battle-card--left";
    public const string BattleCardRight = "battle-card--right";

    public const string AbilityTooltip = "ability-tooltip";
    public const string AbilityTooltipVisible = "ability-tooltip--visible";
    public const string AbilityTooltipText = "ability-tooltip__text";

    public const string ResultSquad = "result-squad";
    public const string ResultSquadDead = "result-squad--dead";
    public const string ResultSquadVisible = "result-squad--visible";
    public const string ResultSquadIcon = "result-squad__icon";
    public const string ResultSquadCountWrapper = "result-squad__count-wrapper";
    public const string ResultSquadCount = "result-squad__count";
    public const string ResultSquadDelta = "result-squad__delta";
    public const string ResultSquadDeltaVisible = "result-squad__delta--visible";
    public const string ResultSquadName = "result-squad__name";
}

public static class BattleSceneElementNames
{
    public const string Body = "Body";
    public const string StartCombatButton = "StartCombatButton";
    public const string LeaveCombatButton = "LeaveCombatButton";
    public const string FinishBattleButton = "FinishBattleButton";
    public const string DefendButton = "DefendButton";
    public const string SkipTurnButton = "SkipTurnButton";
    public const string ResultStatusLabel = "ResultStatusLabel";
    public const string PlayerSquadResults = "PlayerSquadResults";
    public const string EnemySquadResults = "EnemySquadResults";
    public const string QueueList = "QueueList";
    public const string AbilityList = "AbilityList";
    public const string SquadInfoCard = "SquadInfoCard";
    public const string AbilityTooltip = "AbilityTooltip";
}

public sealed class BattleSceneUIController : BaseUIController<BattleSceneElements>
{
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
    public enum PanelName
    {
        TacticPanel,
        CombatPanel,
        ResultPanel
    }

    private const float AbilityTooltipOffset = 8f;
    private const float ResultSquadAnimationDelaySeconds = 0.08f;

    private const string VictoryStatusText = "Победа";
    private const string DefeatStatusText = "Поражение";
    private const string FleeStatusText = "Побег";

    private readonly Dictionary<BattleAbilitySO, List<VisualElement>> _abilityElements = new();
    private BattleAbilitiesManager _currentAbilityManager;
    private IReadOnlySquadModel _currentAbilityOwner;

    private UnitCardWidget _squadInfoCardWidget;
    private IReadOnlySquadModel _displayedSquadModel;
    private BattleSquadEffectsController _displayedEffectsController;

    private Sequence _resultSquadAnimationSequence;
    private VisualElement _abilityTooltipTarget;

    public event Action<BattleAbilitySO> OnSelectAbility;

    private void Update()
    {
        if (!_isAttached)
            return;

        UpdateSquadInfoCardFromPointer();
    }

    protected override void AttachToPanel(UIDocument document)
    {
        base.AttachToPanel(document);

        if (!_isAttached)
            return;
    }

    protected override void DetachFromPanel()
    {
        if (!_isAttached)
            return;

        StopResultSquadAnimation();

        HideAbilityTooltipInternal();
        HideSquadInfoCardInternal();

        _currentAbilityManager = null;
        _currentAbilityOwner = null;
        _abilityElements.Clear();

        _squadInfoCardWidget = null;
        _displayedEffectsController = null;
        _displayedSquadModel = null;
        _abilityTooltipTarget = null;

        base.DetachFromPanel();
    }

    protected override void RegisterUIElements()
    {
        var root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
        if (root == null)
        {
            Debug.LogWarning($"{nameof(BattleSceneUIController)}: UIDocument or rootVisualElement is missing.");
            return;
        }

        _uiElements[BattleSceneElements.Root] = root;

        var body = root.Q<VisualElement>(BattleSceneElementNames.Body);
        if (body == null)
        {
            Debug.LogWarning($"{nameof(BattleSceneUIController)}: Body element was not found in the UI document.");
            return;
        }

        _uiElements[BattleSceneElements.Body] = body;

        _uiElements[BattleSceneElements.StartCombatButton] =
            body.Q<Button>(BattleSceneElementNames.StartCombatButton);
        _uiElements[BattleSceneElements.LeaveCombatButton] =
            body.Q<Button>(BattleSceneElementNames.LeaveCombatButton);
        _uiElements[BattleSceneElements.FinishBattleButton] =
            body.Q<Button>(BattleSceneElementNames.FinishBattleButton);
        _uiElements[BattleSceneElements.DefendButton] =
            body.Q<Button>(BattleSceneElementNames.DefendButton);
        _uiElements[BattleSceneElements.SkipTurnButton] =
            body.Q<Button>(BattleSceneElementNames.SkipTurnButton);

        _uiElements[BattleSceneElements.ResultStatusLabel] =
            body.Q<Label>(BattleSceneElementNames.ResultStatusLabel);

        _uiElements[BattleSceneElements.PlayerSquadResults] =
            body.Q<VisualElement>(BattleSceneElementNames.PlayerSquadResults);
        _uiElements[BattleSceneElements.EnemySquadResults] =
            body.Q<VisualElement>(BattleSceneElementNames.EnemySquadResults);

        var queueContainer = body.Q<VisualElement>(BattleSceneElementNames.QueueList);
        if (queueContainer != null)
        {
            queueContainer.Clear();
            queueContainer.style.display = DisplayStyle.None;
        }
        _uiElements[BattleSceneElements.QueueList] = queueContainer;

        var squadInfoCard = body.Q<VisualElement>(BattleSceneElementNames.SquadInfoCard);
        _uiElements[BattleSceneElements.SquadInfoCard] = squadInfoCard;
        _squadInfoCardWidget = squadInfoCard != null ? new UnitCardWidget(squadInfoCard) : null;
        HideSquadInfoCardInternal();

        var abilityTooltip = new VisualElement { name = BattleSceneElementNames.AbilityTooltip };
        abilityTooltip.AddToClassList(BattleSceneClassNames.AbilityTooltip);

        var abilityTooltipLabel = new Label();
        abilityTooltipLabel.AddToClassList(BattleSceneClassNames.AbilityTooltipText);
        abilityTooltipLabel.enableRichText = true;
        abilityTooltip.Add(abilityTooltipLabel);

        body.Add(abilityTooltip);

        _uiElements[BattleSceneElements.AbilityTooltip] = abilityTooltip;
        _uiElements[BattleSceneElements.AbilityTooltipLabel] = abilityTooltipLabel;

        HideAbilityTooltipInternal();
    }

    protected override void SubcribeToUIEvents()
    {
        var startCombatButton = GetElement<Button>(BattleSceneElements.StartCombatButton);
        var leaveCombatButton = GetElement<Button>(BattleSceneElements.LeaveCombatButton);
        var finishBattleButton = GetElement<Button>(BattleSceneElements.FinishBattleButton);
        var defendButton = GetElement<Button>(BattleSceneElements.DefendButton);
        var skipTurnButton = GetElement<Button>(BattleSceneElements.SkipTurnButton);

        startCombatButton?.RegisterCallback<ClickEvent>(HandleStartCombatClicked);
        leaveCombatButton?.RegisterCallback<ClickEvent>(HandleLeaveCombatClicked);
        finishBattleButton?.RegisterCallback<ClickEvent>(HandleFinishBattleClicked);
        defendButton?.RegisterCallback<ClickEvent>(HandleDefendClicked);
        skipTurnButton?.RegisterCallback<ClickEvent>(HandleSkipTurnClicked);
    }

    protected override void UnsubscriveFromUIEvents()
    {
        var startCombatButton = GetElement<Button>(BattleSceneElements.StartCombatButton);
        var leaveCombatButton = GetElement<Button>(BattleSceneElements.LeaveCombatButton);
        var finishBattleButton = GetElement<Button>(BattleSceneElements.FinishBattleButton);
        var defendButton = GetElement<Button>(BattleSceneElements.DefendButton);
        var skipTurnButton = GetElement<Button>(BattleSceneElements.SkipTurnButton);

        startCombatButton?.UnregisterCallback<ClickEvent>(HandleStartCombatClicked);
        leaveCombatButton?.UnregisterCallback<ClickEvent>(HandleLeaveCombatClicked);
        finishBattleButton?.UnregisterCallback<ClickEvent>(HandleFinishBattleClicked);
        defendButton?.UnregisterCallback<ClickEvent>(HandleDefendClicked);
        skipTurnButton?.UnregisterCallback<ClickEvent>(HandleSkipTurnClicked);
    }

    protected override void SubscriveToGameEvents()
    {
        // No game events for this controller yet.
    }

    protected override void UnsubscribeFromGameEvents()
    {
        // No game events for this controller yet.
    }

    public void ShowPanel(PanelName panelName)
    {
        if (!_isAttached)
            return;

        var body = GetElement<VisualElement>(BattleSceneElements.Body);
        if (body == null)
            return;

        foreach (PanelName name in Enum.GetValues(typeof(PanelName)))
        {
            var panel = body.Q<VisualElement>(name.ToString());
            if (panel == null)
                continue;

            bool isTarget = name == panelName;
            panel.style.display = isTarget ? DisplayStyle.Flex : DisplayStyle.None;
            panel.style.visibility = isTarget ? Visibility.Visible : Visibility.Hidden;
        }
    }

    public void RenderQueue(BattleQueueController battleQueueController)
    {
        if (!_isAttached)
            return;

        var queueContainer = GetElement<VisualElement>(BattleSceneElements.QueueList);
        if (queueContainer == null)
            return;

        queueContainer.Clear();

        if (battleQueueController == null)
        {
            queueContainer.style.display = DisplayStyle.None;
            return;
        }

        IReadOnlyList<IReadOnlySquadModel> queue = battleQueueController.GetQueue();
        if (queue == null || queue.Count == 0)
        {
            queueContainer.style.display = DisplayStyle.None;
            return;
        }

        foreach (IReadOnlySquadModel unit in queue)
        {
            if (unit == null)
                continue;

            VisualElement queueItem = CreateQueueItem(unit);
            queueContainer.Add(queueItem);
        }

        queueContainer.style.display = queueContainer.childCount > 0
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    public void RenderAbilityList(
        BattleAbilitySO[] abilities,
        BattleAbilitiesManager abilityManager,
        IReadOnlySquadModel owner)
    {
        if (!_isAttached)
            return;

        ClearAbilityListInternal();

        _currentAbilityManager = abilityManager;
        _currentAbilityOwner = owner;

        List<VisualElement> containers = GetAbilityListContainers();
        if (abilities == null || abilities.Length == 0 || containers.Count == 0)
        {
            UpdateAbilityListVisibility(false, containers);
            return;
        }

        foreach (BattleAbilitySO ability in abilities)
        {
            if (ability == null)
                continue;

            foreach (VisualElement container in containers)
            {
                if (container == null)
                    continue;

                VisualElement abilityElement = CreateAbilityElement(ability);
                container.Add(abilityElement);
                RegisterAbilityElement(ability, abilityElement);
            }
        }

        UpdateAbilityListVisibility(_abilityElements.Count > 0, containers);
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
                    element.RemoveFromClassList(BattleSceneClassNames.AbilityItemCooldown);
                    element.SetEnabled(true);
                }
                else
                {
                    element.AddToClassList(BattleSceneClassNames.AbilityItemCooldown);
                    element.SetEnabled(false);
                }

                UpdateAbilityCooldownLabel(pair.Key, element, isReady);
            }
        }
    }

    public void HighlightAbility(BattleAbilitySO ability)
    {
        foreach (KeyValuePair<BattleAbilitySO, List<VisualElement>> pair in _abilityElements)
        {
            bool shouldHighlight = ability != null && ReferenceEquals(pair.Key, ability);

            foreach (VisualElement element in pair.Value)
            {
                if (element == null)
                    continue;

                if (shouldHighlight)
                    element.AddToClassList(BattleSceneClassNames.AbilityItemSelected);
                else
                    element.RemoveFromClassList(BattleSceneClassNames.AbilityItemSelected);
            }
        }
    }

    public void ResetAbilityHighlight()
    {
        foreach (KeyValuePair<BattleAbilitySO, List<VisualElement>> pair in _abilityElements)
        {
            foreach (VisualElement element in pair.Value)
            {
                element?.RemoveFromClassList(BattleSceneClassNames.AbilityItemSelected);
            }
        }
    }

    public void SetDefendButtonInteractable(bool interactable)
    {
        var defendButton = GetElement<Button>(BattleSceneElements.DefendButton);
        defendButton?.SetEnabled(interactable);
    }

    public void ShowResult(BattleResult result)
    {
        if (!_isAttached || result == null)
            return;

        StopResultSquadAnimation();

        var statusLabel = GetElement<Label>(BattleSceneElements.ResultStatusLabel);
        if (statusLabel != null)
        {
            statusLabel.text = result.Status switch
            {
                BattleResultStatus.Victory => VictoryStatusText,
                BattleResultStatus.Defeat => DefeatStatusText,
                BattleResultStatus.Flee => FleeStatusText,
                _ => string.Empty
            };
        }

        var friendlyContainer = GetElement<VisualElement>(BattleSceneElements.PlayerSquadResults);
        var enemyContainer = GetElement<VisualElement>(BattleSceneElements.EnemySquadResults);

        List<ResultSquadView> friendlyElements = RenderResultSquads(
            friendlyContainer,
            result.BattleUnitsResult.FriendlyUnits,
            result);

        List<ResultSquadView> enemyElements = RenderResultSquads(
            enemyContainer,
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
        root.AddToClassList(BattleSceneClassNames.ResultSquad);

        var icon = new VisualElement();
        icon.AddToClassList(BattleSceneClassNames.ResultSquadIcon);
        if (squad.Icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(squad.Icon);
        }

        var countWrapper = new VisualElement();
        countWrapper.AddToClassList(BattleSceneClassNames.ResultSquadCountWrapper);

        var countLabel = new Label(Mathf.Max(0, squad.Count).ToString());
        countLabel.AddToClassList(BattleSceneClassNames.ResultSquadCount);
        countWrapper.Add(countLabel);

        Label deltaLabel = CreateResultSquadDeltaLabel(battleResult, squad);
        if (deltaLabel != null)
        {
            countWrapper.Add(deltaLabel);
        }

        var nameLabel = new Label(squad.UnitName ?? string.Empty);
        nameLabel.AddToClassList(BattleSceneClassNames.ResultSquadName);

        root.Add(icon);
        root.Add(countWrapper);
        root.Add(nameLabel);

        if (squad.IsEmpty || squad.Count <= 0)
        {
            root.AddToClassList(BattleSceneClassNames.ResultSquadDead);
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
        label.AddToClassList(BattleSceneClassNames.ResultSquadDelta);
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

            sequence.AppendCallback(() => element.AddToClassList(BattleSceneClassNames.ResultSquadVisible));
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

            sequence.AppendCallback(() => delta.AddToClassList(BattleSceneClassNames.ResultSquadDeltaVisible));
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
        _sceneEventBusService.Publish<RequestStartCombat>(new RequestStartCombat());
    }

    private void HandleLeaveCombatClicked(ClickEvent evt)
    {
        _sceneEventBusService.Publish<RequestFleeCombat>(new RequestFleeCombat());
    }

    private void HandleFinishBattleClicked(ClickEvent evt)
    {
        _sceneEventBusService.Publish<RequestReturnToDungeon>(new RequestReturnToDungeon());
    }

    private void HandleDefendClicked(ClickEvent evt)
    {
        _sceneEventBusService.Publish<RequestDefend>(new RequestDefend());
    }

    private void HandleSkipTurnClicked(ClickEvent evt)
    {
        _sceneEventBusService.Publish<RequestSkipTurn>(new RequestSkipTurn());
    }

    private VisualElement CreateQueueItem(IReadOnlySquadModel unit)
    {
        var item = new VisualElement();
        item.AddToClassList(BattleSceneClassNames.QueueItem);

        var iconElement = new VisualElement();
        iconElement.AddToClassList(BattleSceneClassNames.QueueItemIcon);
        if (unit.Icon != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(unit.Icon);
        }

        var countLabel = new Label(unit.Count.ToString());
        countLabel.AddToClassList(BattleSceneClassNames.QueueItemCount);

        item.Add(iconElement);
        item.Add(countLabel);

        return item;
    }

    private VisualElement CreateAbilityElement(BattleAbilitySO ability)
    {
        var element = new VisualElement();
        element.AddToClassList(BattleSceneClassNames.AbilityItem);

        var iconElement = new VisualElement();
        iconElement.AddToClassList(BattleSceneClassNames.AbilityItemIcon);
        if (ability.Icon != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(ability.Icon);
        }

        element.Add(iconElement);

        var cooldownLabel = new Label();
        cooldownLabel.AddToClassList(BattleSceneClassNames.AbilityItemCooldownLabel);
        cooldownLabel.style.display = DisplayStyle.None;
        element.Add(cooldownLabel);

        element.RegisterCallback<ClickEvent>(_ => HandleAbilityClick(ability));
        element.RegisterCallback<PointerEnterEvent>(_ => ShowAbilityTooltip(ability, element));
        element.RegisterCallback<PointerLeaveEvent>(_ => HideAbilityTooltipInternal());
        element.RegisterCallback<PointerMoveEvent>(_ => UpdateAbilityTooltipFromTarget(element));

        return element;
    }

    private void UpdateAbilityCooldownLabel(BattleAbilitySO ability, VisualElement element, bool isReady)
    {
        if (element == null)
            return;

        var cooldownLabel = element.Q<Label>(className: BattleSceneClassNames.AbilityItemCooldownLabel);
        if (cooldownLabel == null)
            return;

        if (isReady)
        {
            cooldownLabel.style.display = DisplayStyle.None;
            return;
        }

        int remainingCooldown = GetRemainingCooldown(ability);
        if (remainingCooldown <= 0)
        {
            cooldownLabel.style.display = DisplayStyle.None;
            return;
        }

        cooldownLabel.text = remainingCooldown.ToString();
        cooldownLabel.style.display = DisplayStyle.Flex;
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

    private void ClearAbilityListInternal()
    {
        List<VisualElement> containers = GetAbilityListContainers();
        foreach (VisualElement container in containers)
        {
            container?.Clear();
        }

        _abilityElements.Clear();
        HideAbilityTooltipInternal();
    }

    private void UpdateAbilityListVisibility(bool isVisible, List<VisualElement> containers)
    {
        DisplayStyle display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (VisualElement container in containers)
        {
            if (container == null)
                continue;

            container.style.display = display;
        }
    }

    private List<VisualElement> GetAbilityListContainers()
    {
        var body = GetElement<VisualElement>(BattleSceneElements.Body);
        if (body == null)
            return new List<VisualElement>();

        return body.Query<VisualElement>(BattleSceneElementNames.AbilityList).ToList();
    }

    private bool IsAbilityReady(BattleAbilitySO ability)
    {
        if (ability == null)
            return false;

        if (_currentAbilityManager == null || _currentAbilityOwner == null)
            return true;

        return _currentAbilityManager.IsAbilityReady(_currentAbilityOwner, ability);
    }

    private int GetRemainingCooldown(BattleAbilitySO ability)
    {
        if (ability == null)
            return 0;

        if (_currentAbilityManager == null || _currentAbilityOwner == null)
            return 0;

        return Math.Max(0, _currentAbilityManager.GetRemainingCooldown(_currentAbilityOwner, ability));
    }

    private void HandleAbilityClick(BattleAbilitySO ability)
    {
        if (ability == null)
            return;

        if (!IsAbilityReady(ability))
            return;

        OnSelectAbility?.Invoke(ability);
    }

    private void ShowAbilityTooltip(BattleAbilitySO ability, VisualElement target)
    {
        if (ability == null || target == null)
            return;

        var tooltip = GetElement<VisualElement>(BattleSceneElements.AbilityTooltip);
        var tooltipLabel = GetElement<Label>(BattleSceneElements.AbilityTooltipLabel);
        var body = GetElement<VisualElement>(BattleSceneElements.Body);

        if (tooltip == null || tooltipLabel == null || body == null)
            return;

        _abilityTooltipTarget = target;
        tooltipLabel.text = ResolveAbilityTooltipText(ability);
        tooltip.AddToClassList(BattleSceneClassNames.AbilityTooltipVisible);

        UpdateAbilityTooltipPosition(target.worldBound, tooltip, body);

        tooltip.schedule.Execute(() =>
        {
            if (_abilityTooltipTarget == target)
                UpdateAbilityTooltipPosition(target.worldBound, tooltip, body);
        });
    }

    private void UpdateAbilityTooltipFromTarget(VisualElement target)
    {
        if (target == null || !ReferenceEquals(_abilityTooltipTarget, target))
            return;

        var tooltip = GetElement<VisualElement>(BattleSceneElements.AbilityTooltip);
        var body = GetElement<VisualElement>(BattleSceneElements.Body);
        if (tooltip == null || body == null)
            return;

        UpdateAbilityTooltipPosition(target.worldBound, tooltip, body);
    }

    private void UpdateAbilityTooltipPosition(Rect targetWorldBounds, VisualElement tooltip, VisualElement body)
    {
        if (tooltip == null || body == null)
            return;

        Vector2 topCenter = new Vector2(
            targetWorldBounds.xMin + targetWorldBounds.width * 0.5f,
            targetWorldBounds.yMin);

        Vector2 localPosition = body.WorldToLocal(topCenter);

        float tooltipWidth = tooltip.resolvedStyle.width;
        float tooltipHeight = tooltip.resolvedStyle.height;

        float x = localPosition.x - tooltipWidth * 0.5f;
        float y = localPosition.y - tooltipHeight - AbilityTooltipOffset;

        float maxX = Mathf.Max(0f, body.resolvedStyle.width - tooltipWidth);
        x = Mathf.Clamp(x, 0f, maxX);
        y = Mathf.Max(0f, y);

        tooltip.style.left = x;
        tooltip.style.top = y;
    }

    private void HideAbilityTooltipInternal()
    {
        var tooltip = GetElement<VisualElement>(BattleSceneElements.AbilityTooltip);
        if (tooltip == null)
            return;

        tooltip.RemoveFromClassList(BattleSceneClassNames.AbilityTooltipVisible);
        _abilityTooltipTarget = null;
    }

    private static string ResolveAbilityTooltipText(BattleAbilitySO ability)
    {
        if (ability == null)
            return string.Empty;

        string formatted = ability.GetFormatedDescription();
        return string.IsNullOrWhiteSpace(formatted) ? string.Empty : formatted;
    }

    private void UpdateSquadInfoCardFromPointer()
    {
        var squadInfoCard = GetElement<VisualElement>(BattleSceneElements.SquadInfoCard);
        if (squadInfoCard == null || _squadInfoCardWidget == null)
            return;

        BattleSquadController squadController = FindSquadUnderPointer();
        if (squadController == null)
        {
            if (_displayedSquadModel != null)
                HideSquadInfoCardInternal();
            return;
        }

        ShowSquadInfoCardInternal(squadController);
    }

    private void ShowSquadInfoCardInternal(BattleSquadController controller)
    {
        var squadInfoCard = GetElement<VisualElement>(BattleSceneElements.SquadInfoCard);
        if (controller == null || squadInfoCard == null || _squadInfoCardWidget == null)
        {
            HideSquadInfoCardInternal();
            return;
        }

        IReadOnlySquadModel squad = controller.GetSquadModel();
        if (squad == null)
        {
            HideSquadInfoCardInternal();
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

        UpdateSquadInfoContent(squad, effectsController != null ? effectsController.Effects : null);
        UpdateSquadCardPosition(squad, squadInfoCard);

        _squadInfoCardWidget.SetVisible(true);
    }

    private void HideSquadInfoCardInternal()
    {
        UnsubscribeFromDisplayedSquad();
        _displayedEffectsController = null;

        _squadInfoCardWidget?.SetVisible(false);
    }

    private void HandleDisplayedSquadChanged(IReadOnlySquadModel squad)
    {
        if (squad == null || !ReferenceEquals(_displayedSquadModel, squad))
            return;

        UpdateSquadInfoContent(squad, _displayedEffectsController != null ? _displayedEffectsController.Effects : null);
    }

    private void UpdateSquadInfoContent(IReadOnlySquadModel squad, IReadOnlyList<BattleEffectSO> effects)
    {
        if (squad == null || _squadInfoCardWidget == null)
            return;

        IReadOnlyList<BattleAbilitySO> abilities = squad.Abilities ?? Array.Empty<BattleAbilitySO>();
        IReadOnlyList<BattleEffectSO> effectList = effects ?? Array.Empty<BattleEffectSO>();
        Dictionary<string, object> fields = new()
        {
            [UnitCardWidget.LevelFieldKey] = UnitCardWidget.FormatLevelText(squad)
        };

        UnitCardRenderData data = new(
            squad,
            BattleCardStatFields,
            abilities,
            effectList,
            squad.UnitName,
            null,
            fields);

        _squadInfoCardWidget.Render(data);
    }

    private void UpdateSquadCardPosition(IReadOnlySquadModel squad, VisualElement card)
    {
        if (card == null)
            return;

        bool isFriendly = squad.IsFriendly() || squad.IsAlly() || squad.IsHero();
        card.EnableInClassList(BattleSceneClassNames.BattleCardLeft, isFriendly);
        card.EnableInClassList(BattleSceneClassNames.BattleCardRight, !isFriendly);
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
}
