using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UpgradesUIController : MonoBehaviour, ISceneUIController
{
    private const string LevelVisibleClass = "upgrades-level--visible";
    private const string CardVisibleClass = "upgrade-card--visible";
    private const string CardSelectedClass = "upgrade-card--selected";
    private const float RevealAnimationDelaySeconds = 0.12f;

    [SerializeField] private UIDocument _uiDocument;

    private GameEventBusService _sceneEventBusService;
    private InputService _inputService;
    private bool _isAttached = false;
    private VisualElement _root;
    private VisualElement _body;
    private VisualElement _upgradePanel;
    private Label _levelReachedLabel;
    private Sequence _revealSequence;
    private int _selectedUpgradeIndex = -1;

    private InputAction _selectPrevUpgradeAction;
    private InputAction _selectNextUpgradeAction;
    private InputAction _cancelAction;
    private readonly List<UpgradeCard> _upgradeCards = new();

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
    }

    public void AttachToPanel(UIDocument document)
    {
        _root = document.rootVisualElement;
        _body = _root.Q<VisualElement>("Body");
        _upgradePanel = _root.Q<VisualElement>(className: "upgrades-panel");
        _levelReachedLabel = _root.Q<Label>(className: "upgrades-level");

        Hide();
        InitializeUpgradeCard();

        _isAttached = true;
    }

    public void DetachFromPanel()
    {
        _isAttached = false;
        StopRevealAnimation();
        UnsubscribeFromInput();
        _upgradeCards.Clear();
    }

    public void Initialize(GameEventBusService sceneEventBusService, InputService inputService)
    {
        _sceneEventBusService = sceneEventBusService;
        _inputService = inputService;
    }

    public void Show() {
        _body.pickingMode = PickingMode.Position;
        SetPanelActive(_upgradePanel, true);
        SubscribeToInput();
    }

    public void Hide() {
        _body.pickingMode = PickingMode.Ignore;
        SetPanelActive(_upgradePanel, false);
        StopRevealAnimation();
        UnsubscribeFromInput();
    }

    public void SelectUpgrade(UpgradeModel upgradeModel)
    {
        if (upgradeModel == null)
            return;

        UpdateSelectedUpgrade(upgradeModel);
        _sceneEventBusService?.Publish(new SelectSquadUpgrade(upgradeModel));
    }

    public void RenderUpgrades(IReadOnlyList<UpgradeModel> upgradeModels, int level)
    {
        StopRevealAnimation();
        ResetVisibility();
        ResetSelection();
        RenderLevel(level);

        int count = Mathf.Min(_upgradeCards.Count, upgradeModels?.Count ?? 0);
        for (int i = 0; i < _upgradeCards.Count; i++)
        {
            var upgrade = i < count ? upgradeModels[i] : null;
            _upgradeCards[i].Bind(upgrade);
        }

        _selectedUpgradeIndex = GetNextAvailableUpgradeIndex(0);
        UpdateSelectionVisuals();
        AnimateReveal();
    }

    private static void SetPanelActive(VisualElement panel, bool isActive)
    {
        if (panel == null)
            return;

        panel.EnableInClassList("panel__active", isActive);
    }

    private void TryRegisterLifecycleCallbacks()
    {
        if (_uiDocument.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            root.RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

            if (!_isAttached && root.panel != null)
                AttachToPanel(_uiDocument);
        }
    }

    private void HandleAttachToPanel(AttachToPanelEvent evt)
    {
        if (!_isAttached)
            AttachToPanel(_uiDocument);
    }

    private void HandleDetachFromPanel(DetachFromPanelEvent evt)
    {
        DetachFromPanel();
    }

    private void InitializeUpgradeCard()
    {
        _root.Query<VisualElement>(className: "upgrade-card").ForEach(cardElement =>
        {
            if (cardElement == null)
                return;

            _upgradeCards.Add(new UpgradeCard(this, cardElement));
        });
    }

    private void RenderLevel(int level)
    {
        int clampedLevel = Mathf.Max(1, level);
        if (_levelReachedLabel != null)
        {
            _levelReachedLabel.text = $"Достигнут {clampedLevel} уровень!";
        }
    }

    private void AnimateReveal()
    {
        if (_levelReachedLabel == null && _upgradeCards.Count == 0)
            return;

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        if (_levelReachedLabel != null)
        {
            sequence.AppendCallback(() => _levelReachedLabel.AddToClassList(LevelVisibleClass));
            sequence.AppendInterval(RevealAnimationDelaySeconds);
        }

        foreach (UpgradeCard card in _upgradeCards)
        {
            if (card == null)
                continue;

            sequence.AppendCallback(card.Show);
            sequence.AppendInterval(RevealAnimationDelaySeconds);
        }

        _revealSequence = sequence;
    }

    private void StopRevealAnimation()
    {
        if (_revealSequence == null)
            return;

        _revealSequence.Kill();
        _revealSequence = null;
    }

    private void CompleteRevealAnimation()
    {
        StopRevealAnimation();

        _levelReachedLabel?.AddToClassList(LevelVisibleClass);
        foreach (UpgradeCard card in _upgradeCards)
        {
            card?.Show();
        }
    }

    private void ResetVisibility()
    {
        _levelReachedLabel?.RemoveFromClassList(LevelVisibleClass);

        foreach (UpgradeCard card in _upgradeCards)
        {
            card?.ResetVisibility();
        }
    }

    private void SubscribeToInput()
    {
        if (_inputService == null)
            return;

        _selectPrevUpgradeAction = _inputService.Actions.FindAction("SelectPrevUpgrade", throwIfNotFound: false);
        if (_selectPrevUpgradeAction != null)
        {
            _selectPrevUpgradeAction.performed += HandleSelectPrevUpgrade;
        }

        _selectNextUpgradeAction = _inputService.Actions.FindAction("SelectNextUpgrade", throwIfNotFound: false)
            ?? _inputService.Actions.FindAction("SelectNextUpgrades", throwIfNotFound: false);
        if (_selectNextUpgradeAction != null)
        {
            _selectNextUpgradeAction.performed += HandleSelectNextUpgrade;
        }

        _cancelAction = _inputService.Actions.FindAction("Cancel", throwIfNotFound: false);
        if (_cancelAction != null)
        {
            _cancelAction.performed += HandleCancel;
        }
    }

    private void UnsubscribeFromInput()
    {
        if (_selectPrevUpgradeAction != null)
        {
            _selectPrevUpgradeAction.performed -= HandleSelectPrevUpgrade;
            _selectPrevUpgradeAction = null;
        }

        if (_selectNextUpgradeAction != null)
        {
            _selectNextUpgradeAction.performed -= HandleSelectNextUpgrade;
            _selectNextUpgradeAction = null;
        }

        if (_cancelAction != null)
        {
            _cancelAction.performed -= HandleCancel;
            _cancelAction = null;
        }
    }

    private void HandleSelectPrevUpgrade(InputAction.CallbackContext _)
    {
        SelectUpgradeByDirection(-1);
    }

    private void HandleSelectNextUpgrade(InputAction.CallbackContext _)
    {
        SelectUpgradeByDirection(1);
    }

    private void HandleCancel(InputAction.CallbackContext _)
    {
        CompleteRevealAnimation();
    }

    private void SelectUpgradeByDirection(int direction)
    {
        int startIndex = _selectedUpgradeIndex;
        if (startIndex < 0)
            startIndex = direction >= 0 ? 0 : _upgradeCards.Count - 1;

        int targetIndex = GetNextAvailableUpgradeIndex(startIndex + direction, direction);
        if (targetIndex < 0)
            return;

        SelectUpgrade(_upgradeCards[targetIndex].UpgradeModel);
    }

    private int GetNextAvailableUpgradeIndex(int startIndex, int direction = 1)
    {
        if (_upgradeCards.Count == 0)
            return -1;

        int normalizedDirection = direction >= 0 ? 1 : -1;
        int count = _upgradeCards.Count;

        for (int i = 0; i < count; i++)
        {
            int index = (startIndex % count + count) % count;
            if (_upgradeCards[index].HasUpgrade)
                return index;

            startIndex += normalizedDirection;
        }

        return -1;
    }

    private void UpdateSelectedUpgrade(UpgradeModel upgradeModel)
    {
        if (upgradeModel == null)
            return;

        for (int i = 0; i < _upgradeCards.Count; i++)
        {
            if (_upgradeCards[i].UpgradeModel != upgradeModel)
                continue;

            _selectedUpgradeIndex = i;
            UpdateSelectionVisuals();
            break;
        }
    }

    private void ResetSelection()
    {
        _selectedUpgradeIndex = -1;
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < _upgradeCards.Count; i++)
        {
            bool isSelected = i == _selectedUpgradeIndex;
            _upgradeCards[i].UpdateSelected(isSelected);
        }
    }

    private sealed class UpgradeCard
    {
        private readonly UpgradesUIController _controller;
        private readonly VisualElement _card;
        private readonly VisualElement[] _icons;
        private readonly Label _cardInfoText;
        private UpgradeModel _upgradeModel;
        private Clickable _selectUpgradeClickable;

        public UpgradeModel UpgradeModel => _upgradeModel;
        public bool HasUpgrade => _upgradeModel != null;

        public UpgradeCard(UpgradesUIController controller, VisualElement card)
        {
            _controller = controller;
            _card = card;
            _icons = card.Query<VisualElement>(className: "upgrade-card__icon").ToList().ToArray();
            _cardInfoText = card.Q<Label>(className: "upgrade-card__info-text");


            _selectUpgradeClickable = new Clickable(() => controller.SelectUpgrade(_upgradeModel));
            _card.AddManipulator(_selectUpgradeClickable);
        }

        public void Bind(UpgradeModel upgrade)
        {
            _upgradeModel = upgrade;

            if (upgrade?.Target != null)
                _icons[0].style.backgroundImage = new StyleBackground(upgrade.Target.Icon);

            _cardInfoText.text = upgrade?.Description ?? string.Empty;
        }

        public void Show()
        {
            _card?.AddToClassList(CardVisibleClass);
        }

        public void ResetVisibility()
        {
            _card?.RemoveFromClassList(CardVisibleClass);
            _card?.RemoveFromClassList(CardSelectedClass);
        }

        public void UpdateSelected(bool isSelected)
        {
            if (_card == null)
                return;

            _card.EnableInClassList(CardSelectedClass, isSelected);
        }

        public void Dispose()
        {
            _card.RemoveManipulator(_selectUpgradeClickable);
        }
    }
}
