using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UpgradesUIController : MonoBehaviour, ISceneUIController
{
    [SerializeField] private UIDocument _uiDocument;

    private GameEventBusService _sceneEventBusService;
    private bool _isAttached = false;
    private Label _levelReachedLabel;
    private VisualElement _root;
    private VisualElement _body;
    private VisualElement _upgradePanel;
    private readonly List<UpgradeCard> _upgradeCards = new();
    private Sequence _revealSequence;
    private InputAction _cancelAction;

    private const string LevelReachedVisibleClass = "upgrades-level--visible";
    private const string UpgradeCardVisibleClass = "upgrade-card--visible";
    private const float LevelRevealDurationSeconds = 1.5f;
    private const float CardRevealDelaySeconds = 0.3f;

    private static int MinimumLevel => 1;
    private const string CancelActionPath = "UpgradesSelection/Cancel";

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
        UnsubscribeFromInput();
    }

    public void AttachToPanel(UIDocument document)
    {
        _root = document.rootVisualElement;
        _body = _root.Q<VisualElement>("Body");
        _upgradePanel = _root.Q<VisualElement>(className: "upgrades-panel");
        _levelReachedLabel = _root.Q<Label>("LevelLabel");

        Hide();
        InitializeUpgradeCard();

        _isAttached = true;
    }

    public void DetachFromPanel()
    {
        StopRevealAnimation();
        _isAttached = false;
        _upgradeCards.Clear();
    }

    public void Initialize(GameEventBusService sceneEventBusService, InputService inputService)
    {
        _sceneEventBusService = sceneEventBusService;
        _cancelAction = inputService?.Actions?.FindAction(CancelActionPath, false);

        if (_cancelAction != null)
        {
            _cancelAction.performed -= HandleCancelPerformed;
            _cancelAction.performed += HandleCancelPerformed;
        }
    }

    public void Show() {
        _body.pickingMode = PickingMode.Position;
        SetPanelActive(_upgradePanel, true);
    }

    public void Hide() {
        StopRevealAnimation();
        _body.pickingMode = PickingMode.Ignore;
        SetPanelActive(_upgradePanel, false);
    }

    public void ShowWithAnimation(IReadOnlyList<UpgradeModel> upgradeModels, int level)
    {
        RenderUpgrades(upgradeModels);
        UpdateLevelText(level);
        Show();
        PlayRevealAnimation();
    }

    public void SelectUpgrade(UpgradeModel upgradeModel)
    {
        if (upgradeModel == null)
            return;

        _sceneEventBusService?.Publish(new SelectSquadUpgrade(upgradeModel));
    }

    public void RenderUpgrades(IReadOnlyList<UpgradeModel> upgradeModels)
    {
        int count = Mathf.Min(_upgradeCards.Count, upgradeModels?.Count ?? 0);
        for (int i = 0; i < _upgradeCards.Count; i++)
        {
            var upgrade = i < count ? upgradeModels[i] : null;
            _upgradeCards[i].Bind(upgrade);
        }
    }

    private void UpdateLevelText(int level)
    {
        if (_levelReachedLabel == null)
            return;

        int safeLevel = Mathf.Max(MinimumLevel, level);
        _levelReachedLabel.text = $"Достигнут {safeLevel} уровень!";
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

    private void PlayRevealAnimation()
    {
        StopRevealAnimation();
        ResetAnimationState();

        Sequence cardSequence = CreateCardRevealSequence();
        if (cardSequence == null && _levelReachedLabel == null)
            return;

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        sequence.AppendCallback(ShowLevelReachedLabel);
        sequence.AppendInterval(LevelRevealDurationSeconds);

        if (cardSequence != null)
        {
            sequence.AppendCallback(HideLevelReachedLabel);
            sequence.Join(cardSequence);
        }
        else
        {
            sequence.AppendCallback(HideLevelReachedLabel);
        }

        sequence.OnComplete(ApplyFinalAnimationState);
        _revealSequence = sequence;
    }

    private Sequence CreateCardRevealSequence()
    {
        if (_upgradeCards.Count == 0)
            return null;

        Sequence cardSequence = DOTween.Sequence();
        cardSequence.SetUpdate(true);

        for (int i = 0; i < _upgradeCards.Count; i++)
        {
            UpgradeCard upgradeCard = _upgradeCards[i];
            VisualElement cardRoot = upgradeCard.Root;
            if (cardRoot == null)
                continue;

            cardSequence.AppendCallback(() => cardRoot.AddToClassList(UpgradeCardVisibleClass));
            if (i < _upgradeCards.Count - 1)
            {
                cardSequence.AppendInterval(CardRevealDelaySeconds);
            }
        }

        return cardSequence;
    }

    private void ShowLevelReachedLabel()
    {
        _levelReachedLabel?.AddToClassList(LevelReachedVisibleClass);
    }

    private void HideLevelReachedLabel()
    {
        _levelReachedLabel?.RemoveFromClassList(LevelReachedVisibleClass);
    }

    private void ResetAnimationState()
    {
        HideLevelReachedLabel();

        foreach (UpgradeCard upgradeCard in _upgradeCards)
        {
            upgradeCard.SetVisible(false);
        }
    }

    private void StopRevealAnimation()
    {
        if (_revealSequence == null)
            return;

        _revealSequence.Kill();
        _revealSequence = null;
    }

    private void ApplyFinalAnimationState()
    {
        _revealSequence = null;
        HideLevelReachedLabel();

        foreach (UpgradeCard upgradeCard in _upgradeCards)
        {
            upgradeCard.SetVisible(true);
        }
    }

    private void SkipRevealAnimation()
    {
        StopRevealAnimation();
        ApplyFinalAnimationState();
    }

    private void HandleCancelPerformed(InputAction.CallbackContext _)
    {
        SkipRevealAnimation();
    }

    private void UnsubscribeFromInput()
    {
        if (_cancelAction == null)
            return;

        _cancelAction.performed -= HandleCancelPerformed;
        _cancelAction = null;
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

    private sealed class UpgradeCard
    {
        private readonly UpgradesUIController _controller;
        private readonly VisualElement _card;
        private readonly VisualElement[] _icons;
        private readonly Label _cardInfoText;
        private UpgradeModel _upgradeModel;
        private Clickable _selectUpgradeClickable;

        public UpgradeCard(UpgradesUIController controller, VisualElement card)
        {
            _controller = controller;
            _card = card;
            _icons = card.Query<VisualElement>(className: "upgrade-card__icon").ToList().ToArray();
            _cardInfoText = card.Q<Label>(className: "upgrade-card__info-text");


            _selectUpgradeClickable = new Clickable(() => controller.SelectUpgrade(_upgradeModel));
            _card.AddManipulator(_selectUpgradeClickable);
        }

        public VisualElement Root => _card;

        public void Bind(UpgradeModel upgrade)
        {
            _upgradeModel = upgrade;

            if (upgrade?.Target != null)
                _icons[0].style.backgroundImage = new StyleBackground(upgrade.Target.Icon);

            _cardInfoText.text = upgrade?.Description ?? string.Empty;
        }
        
        public void Dispose()
        {
            _card.RemoveManipulator(_selectUpgradeClickable);
        }

        public void SetVisible(bool isVisible)
        {
            _card.EnableInClassList(UpgradeCardVisibleClass, isVisible);
        }
    }
}
