using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PreparationMenuUIController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    public Action<UnitSO, List<UnitSO>> OnDiveIntoCave;

    private readonly List<HeroFrame> _heroFrames = new();
    private readonly List<SquadPanel> _squadPanels = new();

    private VisualElement _root;
    private VisualElement _heroSelectionContainer;
    private VisualElement _heroStatsContainer;
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

        if (_diveIntoCaveButton != null)
            _diveIntoCaveButton.clicked += HandleDiveIntoCave;
    }

    private void OnDisable()
    {
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

        _heroSelectionContainer = _root.Q<VisualElement>("HeroSelection");
        _heroStatsContainer = _root.Q<VisualElement>("HeroStats");
        _diveIntoCaveButton = _root.Q<Button>();

        _heroFrames.Clear();
        _squadPanels.Clear();

        foreach (VisualElement panelElement in _root.Query<VisualElement>(className: "squadPanel"))
        {
            if (panelElement == null)
                continue;

            _squadPanels.Add(new SquadPanel(this, panelElement));
        }

        _initialized = true;
    }

    private void RenderHeroes()
    {
        if (_heroSelectionContainer == null)
            return;

        _heroSelectionContainer.Clear();
        _heroFrames.Clear();

        if (_heroDefinitions.Length == 0)
        {
            _selectedHeroIndex = -1;
            UpdateHeroStats(null);
            return;
        }

        for (int i = 0; i < _heroDefinitions.Length; i++)
        {
            UnitSO heroDefinition = _heroDefinitions[i];
            if (heroDefinition == null)
                continue;

            VisualElement frame = new();
            frame.AddToClassList("heroFrame");
            frame.tooltip = heroDefinition.UnitName;

            if (heroDefinition.Icon != null)
                frame.style.backgroundImage = new StyleBackground(heroDefinition.Icon);
            else
                frame.style.backgroundImage = new StyleBackground();

            int index = i;
            frame.RegisterCallback<ClickEvent>(_ => SelectHero(index));

            _heroFrames.Add(new HeroFrame(frame, i));
            _heroSelectionContainer.Add(frame);
        }

        if (_heroFrames.Count == 0)
        {
            _selectedHeroIndex = -1;
            UpdateHeroStats(null);
            return;
        }

        bool hasSelectedHero = false;
        foreach (HeroFrame heroFrame in _heroFrames)
        {
            if (heroFrame.DefinitionIndex == _selectedHeroIndex)
            {
                hasSelectedHero = true;
                break;
            }
        }

        if (!hasSelectedHero)
            _selectedHeroIndex = _heroFrames[0].DefinitionIndex;

        SelectHero(_selectedHeroIndex);
    }

    private void SelectHero(int index)
    {
        if (_heroFrames.Count == 0 || _heroDefinitions.Length == 0)
        {
            _selectedHeroIndex = -1;
            UpdateHeroStats(null);
            return;
        }

        HeroFrame selectedFrame = null;
        foreach (HeroFrame heroFrame in _heroFrames)
        {
            if (heroFrame.DefinitionIndex == index)
            {
                selectedFrame = heroFrame;
                break;
            }
        }

        if (selectedFrame == null)
        {
            selectedFrame = _heroFrames[0];
            index = selectedFrame.DefinitionIndex;
        }

        _selectedHeroIndex = index;

        foreach (HeroFrame heroFrame in _heroFrames)
        {
            if (heroFrame.Element == null)
                continue;

            if (heroFrame.DefinitionIndex == _selectedHeroIndex)
                heroFrame.Element.AddToClassList("heroFrame_active");
            else
                heroFrame.Element.RemoveFromClassList("heroFrame_active");
        }

        UnitSO selectedHero = index >= 0 && index < _heroDefinitions.Length ? _heroDefinitions[index] : null;
        UpdateHeroStats(selectedHero);
    }

    private void UpdateHeroStats(UnitSO hero)
    {
        if (_heroStatsContainer == null)
            return;

        _heroStatsContainer.Clear();

        if (hero == null)
            return;

        foreach (string stat in BuildUnitStats(hero))
        {
            Label statLabel = new(stat);
            statLabel.AddToClassList("heroStatInfo");
            _heroStatsContainer.Add(statLabel);
        }
    }

    private void RenderSquads()
    {
        if (_squadPanels.Count == 0)
            return;

        if (_squadDefinitions.Length == 0)
        {
            foreach (SquadPanel panel in _squadPanels)
            {
                panel.SelectedIndex = -1;
                ClearSquadPanel(panel);
            }

            return;
        }

        for (int i = 0; i < _squadPanels.Count; i++)
        {
            SquadPanel panel = _squadPanels[i];
            int defaultIndex = panel.SelectedIndex >= 0 ? panel.SelectedIndex : i;
            panel.SelectedIndex = WrapIndex(defaultIndex, _squadDefinitions.Length);
            RenderSquadPanel(panel);
        }
    }

    private void ChangeSquadSelection(SquadPanel panel, int direction)
    {
        if (_squadDefinitions.Length == 0)
            return;

        panel.SelectedIndex = WrapIndex(panel.SelectedIndex + direction, _squadDefinitions.Length);
        RenderSquadPanel(panel);
    }

    private void RenderSquadPanel(SquadPanel panel)
    {
        if (panel.IconElement == null || panel.StatsContainer == null)
            return;

        panel.StatsContainer.Clear();

        if (panel.SelectedIndex < 0 || panel.SelectedIndex >= _squadDefinitions.Length)
        {
            panel.IconElement.style.backgroundImage = new StyleBackground();
            return;
        }

        UnitSO squad = _squadDefinitions[panel.SelectedIndex];

        if (squad != null && squad.Icon != null)
            panel.IconElement.style.backgroundImage = new StyleBackground(squad.Icon);
        else
            panel.IconElement.style.backgroundImage = new StyleBackground();

        panel.IconElement.tooltip = squad != null ? squad.UnitName : string.Empty;

        if (squad == null)
            return;

        foreach (string stat in BuildUnitStats(squad))
        {
            Label statLabel = new(stat);
            statLabel.AddToClassList("squadStatInfo");
            panel.StatsContainer.Add(statLabel);
        }
    }

    private void ClearSquadPanel(SquadPanel panel)
    {
        if (panel.IconElement != null)
            panel.IconElement.style.backgroundImage = new StyleBackground();

        panel.StatsContainer?.Clear();
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

        foreach (SquadPanel panel in _squadPanels)
        {
            if (panel.SelectedIndex < 0 || panel.SelectedIndex >= _squadDefinitions.Length)
                continue;

            UnitSO squad = _squadDefinitions[panel.SelectedIndex];
            if (squad != null)
                selectedSquads.Add(squad);
        }

        return selectedSquads;
    }

    private static IEnumerable<string> BuildUnitStats(UnitSO unit)
    {
        yield return $"Имя: {unit.UnitName}";
        yield return $"Здоровье: {unit.BaseHealth}";
        yield return $"Урон: {unit.MinDamage} - {unit.MaxDamage}";
        yield return $"Физ. защита: {unit.BasePhysicalDefense}";
        yield return $"Скорость: {unit.Speed}";
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

    private sealed class HeroFrame
    {
        public HeroFrame(VisualElement element, int definitionIndex)
        {
            Element = element;
            DefinitionIndex = definitionIndex;
        }

        public VisualElement Element { get; }
        public int DefinitionIndex { get; }
    }

    private sealed class SquadPanel
    {
        private readonly PreparationMenuUIController _controller;

        public SquadPanel(PreparationMenuUIController controller, VisualElement root)
        {
            _controller = controller;
            Root = root;
            IconElement = root?.Q<VisualElement>("Squad");
            StatsContainer = root?.Q<VisualElement>("SquadStats");
            PrevButton = root?.Q<VisualElement>("PrevSquadButton");
            NextButton = root?.Q<VisualElement>("NextSquadButton");

            PrevButton?.RegisterCallback<ClickEvent>(HandlePrevClicked);
            NextButton?.RegisterCallback<ClickEvent>(HandleNextClicked);
        }

        public VisualElement Root { get; }
        public VisualElement IconElement { get; }
        public VisualElement StatsContainer { get; }
        public VisualElement PrevButton { get; }
        public VisualElement NextButton { get; }
        public int SelectedIndex { get; set; } = -1;

        private void HandlePrevClicked(ClickEvent evt)
        {
            evt?.StopPropagation();
            _controller.ChangeSquadSelection(this, -1);
        }

        private void HandleNextClicked(ClickEvent evt)
        {
            evt?.StopPropagation();
            _controller.ChangeSquadSelection(this, 1);
        }
    }
}
