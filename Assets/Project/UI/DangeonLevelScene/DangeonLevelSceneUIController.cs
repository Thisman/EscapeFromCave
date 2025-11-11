using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DangeonLevelSceneUIController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private readonly List<SquadIconEntry> _squadIcons = new();
    private PlayerArmyController _armyController;
    private IReadOnlyArmyModel _currentArmy;
    private bool _initialized;

    private VisualElement _root;
    private VisualElement _squadsContainer;
    private VisualElement _dialogContainer;
    private Label _dialogLabel;

    private void Awake()
    {
        Initialize();
        HideDialog();
    }

    private void OnDestroy()
    {
        BindArmyController(null);
        SubscribeToArmy(null);
    }

    public void BindArmyController(PlayerArmyController controller)
    {
        if (ReferenceEquals(_armyController, controller))
            return;

        if (_armyController != null)
            _armyController.ArmyChanged -= HandleArmyChanged;

        _armyController = controller;

        if (_armyController != null)
        {
            _armyController.ArmyChanged += HandleArmyChanged;
            RenderArmy(_armyController.Army);
        }
        else
        {
            RenderArmy(null);
        }
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
