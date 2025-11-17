using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UICommon.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

public class PreparationMenuUIController : MonoBehaviour, ISceneUIController
{
    [SerializeField] private UIDocument _document;

    public Func<UnitSO, List<SquadSelection>, Task> OnDiveIntoCave;

    private readonly List<HeroCard> _heroCards = new();
    private readonly List<SquadCard> _squadCards = new();

    private VisualElement _root;
    private VisualElement _heroPanel;
    private VisualElement _squadPanel;
    private Button _goToSquadsSelectionButton;
    private Button _diveIntoCaveButton;
    private bool _isDiveRequested;

    private UnitSO[] _heroDefinitions = Array.Empty<UnitSO>();
    private UnitSO[] _squadDefinitions = Array.Empty<UnitSO>();

    private int _selectedHeroIndex = -1;
    private bool _isAttached;

    private static readonly UnitCardStatField[] HeroStatFields =
    {
        UnitCardStatField.Health,
        UnitCardStatField.DamageRange,
        UnitCardStatField.AttackKind,
        UnitCardStatField.DamageType,
        UnitCardStatField.PhysicalDefense,
        UnitCardStatField.MagicDefense,
        UnitCardStatField.AbsoluteDefense,
        UnitCardStatField.CritChance,
        UnitCardStatField.CritMultiplier,
        UnitCardStatField.MissChance
    };

    private static readonly UnitCardStatField[] SquadStatFields =
    {
        //UnitCardStatField.Count,
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

        if (_document?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }
    }

    public void Render(UnitSO[] heroDefinitions, UnitSO[] squadDefinitions)
    {
        _heroDefinitions = heroDefinitions ?? Array.Empty<UnitSO>();
        _squadDefinitions = squadDefinitions ?? Array.Empty<UnitSO>();

        if (!_isAttached)
            return;

        RenderHeroes();
        RenderSquads();
        ShowHeroSelection();
    }

    public void AttachToPanel(UIDocument document)
    {
        if (document == null)
            return;

        if (_isAttached)
            DetachFromPanel();

        _document = document;
        _root = document.rootVisualElement;
        if (_root == null)
            return;

        _heroPanel = _root.Q<VisualElement>("SelectHeroPanel");
        _squadPanel = _root.Q<VisualElement>("SelectSquadsPanel");
        _goToSquadsSelectionButton = _heroPanel?.Q<Button>("GoToSquadsSelection");
        _diveIntoCaveButton = _root.Q<Button>("DiveIntoCaveButton");

        InitializeHeroCards();
        InitializeSquadCards();

        if (_goToSquadsSelectionButton != null)
            _goToSquadsSelectionButton.clicked += HandleGoToSquadsSelection;

        if (_diveIntoCaveButton != null)
            _diveIntoCaveButton.clicked += HandleDiveIntoCaveClicked;

        RenderHeroes();
        RenderSquads();
        ShowHeroSelection();

        _isAttached = true;
    }

    public void DetachFromPanel()
    {
        if (!_isAttached)
            return;

        if (_goToSquadsSelectionButton != null)
        {
            _goToSquadsSelectionButton.clicked -= HandleGoToSquadsSelection;
            _goToSquadsSelectionButton = null;
        }

        if (_diveIntoCaveButton != null)
        {
            _diveIntoCaveButton.clicked -= HandleDiveIntoCaveClicked;
            _diveIntoCaveButton = null;
        }

        foreach (HeroCard card in _heroCards)
            card.Dispose();

        foreach (SquadCard card in _squadCards)
            card.Dispose();

        _heroCards.Clear();
        _squadCards.Clear();

        _heroPanel = null;
        _squadPanel = null;
        _root = null;

        _isAttached = false;
    }

    private void TryRegisterLifecycleCallbacks()
    {
        if (_document == null)
            _document = GetComponent<UIDocument>();

        if (_document?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            root.RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

            if (!_isAttached && root.panel != null)
                AttachToPanel(_document);
        }
    }

    private void HandleAttachToPanel(AttachToPanelEvent _)
    {
        if (!_isAttached)
            AttachToPanel(_document);
    }

    private void HandleDetachFromPanel(DetachFromPanelEvent _)
    {
        DetachFromPanel();
    }

    private void InitializeHeroCards()
    {
        _heroCards.Clear();

        VisualElement content = _heroPanel?.Q<VisualElement>("Content");
        content?.Query<VisualElement>(className: UnitCardWidget.BlockClassName).ForEach(cardElement =>
        {
            if (cardElement == null)
                return;

            _heroCards.Add(new HeroCard(this, cardElement));
        });
    }

    private void InitializeSquadCards()
    {
        _squadCards.Clear();

        VisualElement content = _squadPanel?.Q<VisualElement>("Content");
        content?.Query<VisualElement>(name: "SquadsContainer").ForEach(cardElement =>
        {
            if (cardElement == null)
                return;

            _squadCards.Add(new SquadCard(this, cardElement));
        });
    }

    private void RenderHeroes()
    {
        if (_heroCards.Count == 0)
        {
            _selectedHeroIndex = -1;
            return;
        }

        _selectedHeroIndex = _heroDefinitions.Length > 0 ? 0 : -1;

        for (int i = 0; i < _heroCards.Count; i++)
        {
            HeroCard card = _heroCards[i];
            UnitSO hero = i < _heroDefinitions.Length ? _heroDefinitions[i] : null;
            IReadOnlyList<UnitCardStatField> stats = hero != null ? HeroStatFields : Array.Empty<UnitCardStatField>();
            int definitionIndex = hero != null ? i : -1;

            card.Bind(hero, definitionIndex, stats);
        }

        UpdateHeroSelection();
    }

    private void RenderSquads()
    {
        if (_squadCards.Count == 0)
            return;

        if (_squadDefinitions.Length == 0)
        {
            foreach (SquadCard card in _squadCards)
                card.UpdateContent(null, -1, Array.Empty<UnitCardStatField>());

            return;
        }

        for (int i = 0; i < _squadCards.Count; i++)
        {
            SquadCard card = _squadCards[i];
            int index = card.HasValidSelection(_squadDefinitions.Length)
                ? card.SelectedDefinitionIndex
                : WrapIndex(i, _squadDefinitions.Length);

            SetSquadCardContent(card, index);
        }
    }

    private void ChangeSquadSelection(SquadCard card, int direction)
    {
        if (card == null || _squadDefinitions.Length == 0)
            return;

        int newIndex;
        if (card.HasValidSelection(_squadDefinitions.Length))
            newIndex = WrapIndex(card.SelectedDefinitionIndex + direction, _squadDefinitions.Length);
        else
            newIndex = direction >= 0 ? 0 : _squadDefinitions.Length - 1;

        SetSquadCardContent(card, newIndex);
    }

    private void SetSquadCardContent(SquadCard card, int index)
    {
        if (card == null)
            return;

        UnitSO squad = index >= 0 && index < _squadDefinitions.Length ? _squadDefinitions[index] : null;
        IReadOnlyList<UnitCardStatField> stats = squad != null ? SquadStatFields : Array.Empty<UnitCardStatField>();
        card.UpdateContent(squad, squad != null ? index : -1, stats);
    }

    private void SelectHero(int index)
    {
        if (_heroDefinitions.Length == 0)
        {
            _selectedHeroIndex = -1;
        }
        else
        {
            if (index < 0 || index >= _heroDefinitions.Length)
                index = 0;

            _selectedHeroIndex = index;
        }

        UpdateHeroSelection();
    }

    private void UpdateHeroSelection()
    {
        foreach (HeroCard card in _heroCards)
        {
            bool isSelected = card.DefinitionIndex >= 0 && card.DefinitionIndex == _selectedHeroIndex;
            card.UpdateSelected(isSelected);
        }
    }

    private void ShowHeroSelection()
    {
        SetPanelActive(_heroPanel, true);
        SetPanelActive(_squadPanel, false);
    }

    private void ShowSquadSelection()
    {
        SetPanelActive(_heroPanel, false);
        SetPanelActive(_squadPanel, true);
    }

    private static void SetPanelActive(VisualElement panel, bool isActive)
    {
        if (panel == null)
            return;

        panel.EnableInClassList("panel__active", isActive);
    }

    private void HandleGoToSquadsSelection()
    {
        ShowSquadSelection();
    }

    private void HandleDiveIntoCaveClicked()
    {
        _ = RequestDiveIntoCaveAsync();
    }

    private async Task RequestDiveIntoCaveAsync()
    {
        if (_isDiveRequested)
        {
            Debug.Log("Dive into cave request is already running", this);
            return;
        }

        UnitSO selectedHero = GetSelectedHero();
        List<SquadSelection> selectedSquads = GetSelectedSquads();

        if (_diveIntoCaveButton != null)
            _diveIntoCaveButton.SetEnabled(false);

        _isDiveRequested = true;

        try
        {
            if (OnDiveIntoCave != null)
            {
                Debug.Log("[PreparationMenuUI] Starting dive into cave", this);
                await OnDiveIntoCave.Invoke(selectedHero, selectedSquads);
                Debug.Log("[PreparationMenuUI] Dive into cave finished", this);
            }
            else
            {
                Debug.LogWarning("OnDiveIntoCave handler is not assigned", this);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to dive into cave: {exception}", this);
        }
        finally
        {
            _isDiveRequested = false;

            if (_diveIntoCaveButton != null)
                _diveIntoCaveButton.SetEnabled(true);
        }
    }

    private UnitSO GetSelectedHero()
    {
        if (_selectedHeroIndex < 0 || _selectedHeroIndex >= _heroDefinitions.Length)
            return null;

        return _heroDefinitions[_selectedHeroIndex];
    }

    private List<SquadSelection> GetSelectedSquads()
    {
        List<SquadSelection> selectedSquads = new();

        foreach (SquadCard card in _squadCards)
        {
            int index = card.SelectedDefinitionIndex;
            if (index < 0 || index >= _squadDefinitions.Length)
                continue;

            UnitSO squad = _squadDefinitions[index];
            if (squad == null)
                continue;

            int defaultCount = Mathf.RoundToInt(GetDefaultCount(squad));
            selectedSquads.Add(new SquadSelection(squad, defaultCount));
        }

        return selectedSquads;
    }

    private static float GetDefaultCount(UnitSO _)
    {
        return 10f;
    }

    private static int WrapIndex(int index, int length)
    {
        if (length <= 0)
            return -1;

        int result = index % length;
        if (result < 0)
            result += length;

        return result;
    }

    private sealed class HeroCard
    {
        private readonly PreparationMenuUIController _controller;
        private readonly UnitCardWidget _card;
        private readonly Clickable _clickable;

        public HeroCard(PreparationMenuUIController controller, VisualElement root)
        {
            _controller = controller;
            if (root != null)
            {
                _card = new UnitCardWidget(root);
                _clickable = new Clickable(HandleClick);
                _card.Root.AddManipulator(_clickable);
            }
        }

        public int DefinitionIndex { get; private set; } = -1;

        public void Bind(UnitSO hero, int definitionIndex, IReadOnlyList<UnitCardStatField> stats)
        {
            DefinitionIndex = definitionIndex;
            if (_card == null)
                return;

            IReadOnlyList<UnitCardStatField> statFields = hero != null ? stats : Array.Empty<UnitCardStatField>();
            IReadOnlySquadModel squad = hero != null ? new SquadModel(hero, Mathf.RoundToInt(GetDefaultCount(hero))) : null;
            IReadOnlyList<BattleAbilitySO> abilities = hero?.Abilities ?? Array.Empty<BattleAbilitySO>();

            UnitCardRenderData data = new(squad, statFields, abilities, Array.Empty<BattleEffectSO>(), hero?.UnitName);
            _card.Render(data);
            _card.SetEnabled(hero != null);
        }

        public void UpdateSelected(bool isSelected)
        {
            _card?.SetSelected(isSelected);
        }

        public void Dispose()
        {
            if (_card?.Root != null && _clickable != null)
                _card.Root.RemoveManipulator(_clickable);
        }

        private void HandleClick()
        {
            if (DefinitionIndex < 0)
                return;

            _controller.SelectHero(DefinitionIndex);
        }
    }

    private sealed class SquadCard
    {
        private readonly PreparationMenuUIController _controller;
        private readonly VisualElement _prevButton;
        private readonly VisualElement _nextButton;
        private readonly Clickable _prevClickable;
        private readonly Clickable _nextClickable;
        private readonly UnitCardWidget _card;

        public SquadCard(PreparationMenuUIController controller, VisualElement root)
        {
            _controller = controller;
            _prevButton = root?.Q<VisualElement>("PrevButton");
            _nextButton = root?.Q<VisualElement>("NextButton");
            VisualElement cardRoot = root?.Q<VisualElement>("Card");
            if (cardRoot != null)
                _card = new UnitCardWidget(cardRoot);

            if (_prevButton != null)
            {
                _prevClickable = new Clickable(() => _controller.ChangeSquadSelection(this, -1));
                _prevButton.AddManipulator(_prevClickable);
            }

            if (_nextButton != null)
            {
                _nextClickable = new Clickable(() => _controller.ChangeSquadSelection(this, 1));
                _nextButton.AddManipulator(_nextClickable);
            }
        }

        public int SelectedDefinitionIndex { get; private set; } = -1;

        public void UpdateContent(UnitSO squad, int index, IReadOnlyList<UnitCardStatField> stats)
        {
            SelectedDefinitionIndex = index;
            if (_card == null)
                return;

            IReadOnlyList<UnitCardStatField> statFields = squad != null ? stats : Array.Empty<UnitCardStatField>();
            IReadOnlySquadModel squadModel = squad != null ? new SquadModel(squad, Mathf.RoundToInt(GetDefaultCount(squad))) : null;
            IReadOnlyList<BattleAbilitySO> abilities = squad?.Abilities ?? Array.Empty<BattleAbilitySO>();

            UnitCardRenderData data = new(squadModel, statFields, abilities, Array.Empty<BattleEffectSO>(), squad?.UnitName);
            _card.Render(data);
            _card.SetEnabled(squad != null);
        }

        public bool HasValidSelection(int definitionsLength)
        {
            return definitionsLength > 0 && SelectedDefinitionIndex >= 0 && SelectedDefinitionIndex < definitionsLength;
        }

        public void Dispose()
        {
            if (_prevButton != null && _prevClickable != null)
                _prevButton.RemoveManipulator(_prevClickable);

            if (_nextButton != null && _nextClickable != null)
                _nextButton.RemoveManipulator(_nextClickable);
        }
    }
}
