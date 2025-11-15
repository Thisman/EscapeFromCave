using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PreparationMenuUIController : MonoBehaviour
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
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();

        if (_goToSquadsSelectionButton != null)
            _goToSquadsSelectionButton.clicked += HandleGoToSquadsSelection;

        if (_diveIntoCaveButton != null)
            _diveIntoCaveButton.clicked += HandleDiveIntoCave;

        ShowHeroSelection();
    }

    private void OnDisable()
    {
        if (_goToSquadsSelectionButton != null)
            _goToSquadsSelectionButton.clicked -= HandleGoToSquadsSelection;

        if (_diveIntoCaveButton != null)
            _diveIntoCaveButton.clicked -= HandleDiveIntoCave;
    }

    public void Render(UnitSO[] heroDefinitions, UnitSO[] squadDefinitions)
    {
        _heroDefinitions = heroDefinitions ?? Array.Empty<UnitSO>();
        _squadDefinitions = squadDefinitions ?? Array.Empty<UnitSO>();

        Initialize();
        RenderHeroes();
        RenderSquads();
        ShowHeroSelection();
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

        _heroPanel = _root.Q<VisualElement>("SelectHeroPanel");
        _squadPanel = _root.Q<VisualElement>("SelectSquadsPanel");
        _goToSquadsSelectionButton = _heroPanel?.Q<Button>("GoToSquadsSelection");
        _diveIntoCaveButton = _root.Q<Button>("DiveIntoCaveButton");

        InitializeHeroCards();
        InitializeSquadCards();

        _initialized = true;
    }

    private void InitializeHeroCards()
    {
        _heroCards.Clear();

        VisualElement content = _heroPanel?.Q<VisualElement>("Content");
        content?.Query<VisualElement>(className: "card").ForEach(cardElement =>
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
        content?.Query<VisualElement>(className: "card").ForEach(cardElement =>
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

        List<string> entries = new()
        {
            $"Название: {unit.UnitName}",
            $"Количество: {FormatValue(GetDefaultCount(unit))}",
            $"Здоровье: {FormatValue(unit.BaseHealth)}",
            $"Физическая защита: {FormatPercent(unit.BasePhysicalDefense)}",
            $"Магическая защита: {FormatPercent(unit.BaseMagicDefense)}",
            $"Абсолютная защита: {FormatPercent(unit.BaseAbsoluteDefense)}",
            $"Тип атаки: {FormatAttackKind(unit.AttackKind)}",
            $"Тип урона: {FormatDamageType(unit.DamageType)}",
        };

        (float minDamage, float maxDamage) = unit.GetBaseDamageRange();
        entries.Add($"Урон: {FormatValue(minDamage)} - {FormatValue(maxDamage)}");
        entries.Add($"Скорость: {FormatValue(unit.Speed)}");
        entries.Add($"Инициатива: {FormatValue(unit.Speed)}");
        entries.Add($"Шанс критического удара: {FormatPercent(unit.BaseCritChance)}");
        entries.Add($"Критический множитель: {FormatValue(unit.BaseCritMultiplier)}");
        entries.Add($"Шанс промаха: {FormatPercent(unit.BaseMissChance)}");

        return entries;
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

    private abstract class CardView
    {
        private readonly List<Label> _infoLabels = new();

        protected CardView(VisualElement root)
        {
            Root = root;
            Icon = root?.Q<VisualElement>("Icon");
            Title = root?.Q<Label>("Title");

            VisualElement infoContainer = root?.Q<VisualElement>("Info");
            infoContainer?.Query<Label>().ForEach(label =>
            {
                if (label == null)
                    return;

                _infoLabels.Add(label);
            });
        }

        protected void ApplyUnit(UnitSO unit, IReadOnlyList<string> stats)
        {
            if (Icon != null)
            {
                if (unit != null && unit.Icon != null)
                    Icon.style.backgroundImage = new StyleBackground(unit.Icon);
                else
                    Icon.style.backgroundImage = new StyleBackground();

                Icon.tooltip = unit != null ? unit.UnitName : string.Empty;
            }

            if (Title != null)
                Title.text = unit != null ? unit.UnitName : string.Empty;

            int statsCount = stats?.Count ?? 0;
            for (int i = 0; i < _infoLabels.Count; i++)
            {
                Label label = _infoLabels[i];
                if (label == null)
                    continue;

                if (i < statsCount)
                {
                    label.text = stats[i];
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.text = string.Empty;
                    label.style.display = DisplayStyle.None;
                }
            }

            Root?.EnableInClassList("card__selected", false);
        }

        protected VisualElement Root { get; }
        protected VisualElement Icon { get; }
        protected Label Title { get; }
    }

    private sealed class HeroCard : CardView
    {
        private readonly PreparationMenuUIController _controller;

        public HeroCard(PreparationMenuUIController controller, VisualElement root)
            : base(root)
        {
            _controller = controller;

            if (Root != null)
                Root.AddManipulator(new Clickable(HandleClick));
        }

        public int DefinitionIndex { get; private set; } = -1;

        public void Bind(UnitSO hero, int definitionIndex, IReadOnlyList<string> stats)
        {
            DefinitionIndex = definitionIndex;
            ApplyUnit(hero, stats);
            Root?.SetEnabled(hero != null);
        }

        public void UpdateSelected(bool isSelected)
        {
            Root?.EnableInClassList("card__selected", isSelected);
        }

        private void HandleClick()
        {
            if (DefinitionIndex < 0)
                return;

            _controller.SelectHero(DefinitionIndex);
        }
    }

    private sealed class SquadCard : CardView
    {
        private readonly PreparationMenuUIController _controller;
        private readonly VisualElement _prevButton;
        private readonly VisualElement _nextButton;

        public SquadCard(PreparationMenuUIController controller, VisualElement root)
            : base(root)
        {
            _controller = controller;
            _prevButton = root?.Q<VisualElement>("PrevButton");
            _nextButton = root?.Q<VisualElement>("NextButton");

            if (_prevButton != null)
                _prevButton.AddManipulator(new Clickable(() => _controller.ChangeSquadSelection(this, -1)));

            if (_nextButton != null)
                _nextButton.AddManipulator(new Clickable(() => _controller.ChangeSquadSelection(this, 1)));
        }

        public int SelectedDefinitionIndex { get; private set; } = -1;

        public void UpdateContent(UnitSO squad, int index, IReadOnlyList<string> stats)
        {
            SelectedDefinitionIndex = index;
            ApplyUnit(squad, stats);
            Root?.SetEnabled(squad != null);
        }

        public bool HasValidSelection(int definitionsLength)
        {
            return definitionsLength > 0 && SelectedDefinitionIndex >= 0 && SelectedDefinitionIndex < definitionsLength;
        }
    }
}
