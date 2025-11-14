using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BattleUIController : MonoBehaviour
{
    private const string BodyElementName = "Body";
    private const string StartCombatButtonName = "StartCombatButton";
    private const string LeaveCombatButtonName = "LeaveCombatButton";
    private const string FinishBattleButtonName = "FinishBattleButton";
    private const string ResultStatusLabelName = "ResultStatusLabel";

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
        // TODO: implement queue rendering when UI is ready.
    }

    public void RenderAbilityList(BattleAbilitySO[] abilities, BattleAbilityManager abilityManager, IReadOnlySquadModel owner)
    {
        // TODO: implement ability buttons rendering when UI is ready.
    }

    public void RefreshAbilityAvailability()
    {
        // TODO: update ability availability state when UI is ready.
    }

    public void HighlightAbility(BattleAbilitySO ability)
    {
        // TODO: implement ability highlighting when UI is ready.
    }

    public void ResetAbilityHighlight()
    {
        // TODO: reset ability highlighting when UI is ready.
    }

    public void SetDefendButtonInteractable(bool interactable)
    {
        if (_defendButton == null)
            return;

        _defendButton.SetEnabled(interactable);
    }

    public void ShowResult(BattleResult result)
    {
        if (_resultStatusLabel == null || result == null)
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
}
