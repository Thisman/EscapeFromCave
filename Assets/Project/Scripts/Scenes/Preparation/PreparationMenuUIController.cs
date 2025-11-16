using System;
using System.Collections.Generic;
using UICommon.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

public class PreparationMenuUIController : MonoBehaviour, ISceneUIController
{
    [SerializeField] private UIDocument _document;

    public Action<UnitSO, List<UnitSO>> OnDiveIntoCave;

    private readonly List<HeroCard> _heroCards = new();
    private readonly List<SquadCard> _squadCards = new();

    private VisualElement _root;
    private VisualElement _heroPanel;
    private VisualElement _squadPanel;
    private Button _goToSquadsSelectionButton;
    private Button _diveIntoCaveButton;

    private UnitSO[] _heroDefinitions = Array.Empty<UnitSO>();
    private UnitSO[] _squadDefinitions = Array.Empty<UnitSO>();

    private int _selectedHeroIndex = -1;
    private bool _isAttached;

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
            _diveIntoCaveButton.clicked += HandleDiveIntoCave;

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
            _diveIntoCaveButton.clicked -= HandleDiveIntoCave;
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
            IReadOnlyList<string> stats = hero != null ? BuildUnitStatEntries(hero) : Array.Empty<string>();
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
                card.UpdateContent(null, -1, Array.Empty<string>());

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
        IReadOnlyList<string> stats = squad != null ? BuildUnitStatEntries(squad) : Array.Empty<string>();
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

    private void HandleDiveIntoCave()
    {
        UnitSO selectedHero = GetSelectedHero();
        List<UnitSO> selectedSquads = GetSelectedSquads();
        OnDiveIntoCave?.Invoke(selectedHero, selectedSquads);
    }

    private UnitSO GetSelectedHero()
    {
        if (_selectedHeroIndex < 0 || _selectedHeroIndex >= _heroDefinitions.Length)
            return null;

        return _heroDefinitions[_selectedHeroIndex];
    }

    private List<UnitSO> GetSelectedSquads()
    {
        List<UnitSO> selectedSquads = new();

        foreach (SquadCard card in _squadCards)
        {
            int index = card.SelectedDefinitionIndex;
            if (index < 0 || index >= _squadDefinitions.Length)
                continue;

            UnitSO squad = _squadDefinitions[index];
            if (squad != null)
                selectedSquads.Add(squad);
        }

        return selectedSquads;
    }

    private static IReadOnlyList<string> BuildUnitStatEntries(UnitSO unit)
    {
        if (unit == null)
            return Array.Empty<string>();

        (float minDamage, float maxDamage) = unit.GetBaseDamageRange();
        List<string> entries = new()
        {
            $"Здоровье: {FormatValue(unit.BaseHealth)}",
            $"Урон: {FormatValue(minDamage)} - {FormatValue(maxDamage)}",
            $"Тип атаки: {FormatAttackKind(unit.AttackKind)}",
            $"Тип урона: {FormatDamageType(unit.DamageType)}",
            $"Физическая защита: {FormatPercent(unit.BasePhysicalDefense)}",
            $"Магическая защита: {FormatPercent(unit.BaseMagicDefense)}",
            $"Абсолютная защита: {FormatPercent(unit.BaseAbsoluteDefense)}",
            $"Шанс критического удара: {FormatPercent(unit.BaseCritChance)}",
            $"Критический множитель: {FormatValue(unit.BaseCritMultiplier)}",
            $"Шанс промаха: {FormatPercent(unit.BaseMissChance)}",
        };

        return entries;
    }

    private static IReadOnlyList<UnitCardIconRenderData> BuildAbilityIcons(BattleAbilitySO[] abilities)
    {
        if (abilities == null || abilities.Length == 0)
            return Array.Empty<UnitCardIconRenderData>();

        List<UnitCardIconRenderData> result = new();

        foreach (BattleAbilitySO ability in abilities)
        {
            if (ability?.Icon == null)
                continue;

            string tooltip = ability.AbilityName ?? string.Empty;
            if (!string.IsNullOrEmpty(ability.Description))
            {
                tooltip = string.IsNullOrEmpty(tooltip)
                    ? ability.Description
                    : $"{tooltip}\n{ability.Description}";
            }

            result.Add(new UnitCardIconRenderData(ability.Icon, tooltip));
        }

        return result;
    }

    private static float GetDefaultCount(UnitSO _)
    {
        return 1f;
    }

    private static string FormatValue(float value)
    {
        return value.ToString("0.##");
    }

    private static string FormatPercent(float value)
    {
        return value.ToString("P0");
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

        public void Bind(UnitSO hero, int definitionIndex, IReadOnlyList<string> stats)
        {
            DefinitionIndex = definitionIndex;
            if (_card == null)
                return;

            string title = hero != null ? hero.UnitName : string.Empty;
            IReadOnlyList<UnitCardIconRenderData> abilities = BuildAbilityIcons(hero?.Abilities);
            UnitCardRenderData data = new(title, hero != null ? hero.Icon : null, stats, title, abilities);
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

        public void UpdateContent(UnitSO squad, int index, IReadOnlyList<string> stats)
        {
            SelectedDefinitionIndex = index;
            if (_card == null)
                return;

            string title = squad != null ? squad.UnitName : string.Empty;
            IReadOnlyList<UnitCardIconRenderData> abilities = BuildAbilityIcons(squad?.Abilities);
            UnitCardRenderData data = new(title, squad != null ? squad.Icon : null, stats, title, abilities);
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
