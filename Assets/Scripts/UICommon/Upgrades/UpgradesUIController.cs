using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UpgradesUIController : MonoBehaviour, ISceneUIController
{
    [SerializeField] private UIDocument _uiDocument;

    private bool _isAttached = false;
    private VisualElement _root;
    private VisualElement _body;
    private VisualElement _upgradePanel;
    private readonly List<UpgradeCard> _upgradeCards = new();

    public Action<UpgradeModel> OnSelectUpgrade;

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

        Hide();
        InitializeUpgradeCard();

        _isAttached = true;
    }

    public void DetachFromPanel()
    {
        _isAttached = false;
        _upgradeCards.Clear();
    }

    public void Show() {
        _body.pickingMode = PickingMode.Position;
        SetPanelActive(_upgradePanel, true);
    }

    public void Hide() {
        _body.pickingMode = PickingMode.Ignore;
        SetPanelActive(_upgradePanel, false);
    }

    public void SelectUpgrade(UpgradeModel upgradeModel)
    {
        OnSelectUpgrade?.Invoke(upgradeModel);
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
    }
}
