using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UICommon.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public enum PreparationSceneUIElements
{
    Root,
    HeroPanel,
    SquadsPanel,
    SquadsList,
    GoToSquadsSelectionButton,
    GoToHeroSelectionButton,
    DiveIntoCaveButton,
    IncreaseSquadCountButton,
    DecreaseSquadCountButton,
    SquadCountLabel,
    TotalUnitsLabel,
}

public static class PreparationSceneClassNames
{
    public const string SquadSlotActive = "squad-item--active";
    public const string SquadSlotLocked = "squad-item--locked";
    public const string SquadSlotHero = "squad-item--hero";
    public const string SquadSlotCount = "squad-item__count";
    public const string SquadItem = "squad-item";
}

public class PreparationSceneUIController : BaseUIController<PreparationSceneUIElements>
{
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

    private InputService _inputService;

    private readonly List<HeroCard> _heroCards = new();
    private readonly List<SquadCard> _squadCards = new();
    private UnitSO[] _heroDefinitions = Array.Empty<UnitSO>();
    private UnitSO[] _squadDefinitions = Array.Empty<UnitSO>();

    private int _selectedHeroIndex = -1;
    private bool _isHeroSelectionVisible;

    private InputAction _selectPrevHeroAction;
    private InputAction _selectNextHeroAction;

    private int _totalSquadUnits;
    private SquadSlot _activeSquadSlot;
    private readonly List<SquadSlot> _squadSlots = new();
    private Clickable _decreaseSquadCountClickable;
    private Clickable _increaseSquadCountClickable;

    private const int DefaultSquadUnitCount = 10;
    private const int MinSquadUnitCount = 1;
    private const int MaxSquadUnitsLimit = 30;

    public void Initialize(InputService inputService, GameEventBusService _sceneEventBusService)
    {
        base.Initialize(_sceneEventBusService);

        _inputService = inputService;

        InitializeHeroCards();
        InitializeSquadCards();
        InitializeSquadSlots();
        SubscribeToHeroNavigationInput();
    }

    public void Render(UnitSO[] heroDefinitions, UnitSO[] squadDefinitions)
    {
        _heroDefinitions = heroDefinitions;
        _squadDefinitions = squadDefinitions;

        RenderHeroes();
        RenderSquads();

        UpdateHeroSlot();
        UpdateSquadCardFromActiveSlot();
        UpdateSquadCountControls();
        UpdateSlotStates();

        ShowHeroSelection();
    }

    override protected void DetachFromPanel()
    {
        base.DetachFromPanel();

        UnsubscribeFromHeroNavigationInput();

        foreach (HeroCard card in _heroCards)
            card.Dispose();

        foreach (SquadCard card in _squadCards)
            card.Dispose();

        foreach (SquadSlot slot in _squadSlots)
            slot.Dispose();

        _heroCards.Clear();
        _squadCards.Clear();
        _squadSlots.Clear();
        _totalSquadUnits = 0;
        _activeSquadSlot = null;
        _isHeroSelectionVisible = false;
    }

    override protected void RegisterUIElements()
    {
        var root = _uiDocument.rootVisualElement;
        _uiElements[PreparationSceneUIElements.Root] = root;
        _uiElements[PreparationSceneUIElements.HeroPanel] = root.Q<VisualElement>("SelectHeroPanel");
        _uiElements[PreparationSceneUIElements.SquadsPanel] = root.Q<VisualElement>("SelectSquadsPanel");
        _uiElements[PreparationSceneUIElements.SquadsList] = root.Q<VisualElement>("SquadsList");
        _uiElements[PreparationSceneUIElements.TotalUnitsLabel] = root.Q<Label>("SquadsTotalUnitCount");
        _uiElements[PreparationSceneUIElements.SquadCountLabel] = root.Q<Label>("SquadCountLabel");

        _uiElements[PreparationSceneUIElements.DiveIntoCaveButton] = root.Q<Button>("DiveIntoCaveButton");
        _uiElements[PreparationSceneUIElements.GoToHeroSelectionButton] = root.Q<Button>("GoToHeroSelection");
        _uiElements[PreparationSceneUIElements.GoToSquadsSelectionButton] = root.Q<Button>("GoToSquadsSelection");
        _uiElements[PreparationSceneUIElements.DecreaseSquadCountButton] = root.Q<VisualElement>("DecreaseUnitCountButton");
        _uiElements[PreparationSceneUIElements.IncreaseSquadCountButton] = root.Q<VisualElement>("IncreaseUnitCountButton");
    }

    override protected void SubcribeToUIEvents()
    {
        GetElement<Button>(PreparationSceneUIElements.GoToSquadsSelectionButton).clicked += HandleGoToSquadsSelection;
        GetElement<Button>(PreparationSceneUIElements.GoToHeroSelectionButton).clicked += HandleGoToHeroSelection;
        GetElement<Button>(PreparationSceneUIElements.DiveIntoCaveButton).clicked += HandleDiveIntoCaveClicked;

        _decreaseSquadCountClickable = new Clickable(() => ChangeActiveSlotCount(-1));
        GetElement<VisualElement>(PreparationSceneUIElements.DecreaseSquadCountButton).AddManipulator(_decreaseSquadCountClickable);

        _increaseSquadCountClickable = new Clickable(() => ChangeActiveSlotCount(1));
        GetElement<VisualElement>(PreparationSceneUIElements.IncreaseSquadCountButton).AddManipulator(_increaseSquadCountClickable);
    }

    override protected void UnsubscriveFromUIEvents()
    {
        GetElement<Button>(PreparationSceneUIElements.GoToSquadsSelectionButton).clicked -= HandleGoToSquadsSelection;
        GetElement<Button>(PreparationSceneUIElements.GoToHeroSelectionButton).clicked -= HandleGoToHeroSelection;
        GetElement<Button>(PreparationSceneUIElements.DiveIntoCaveButton).clicked -= HandleDiveIntoCaveClicked;

        GetElement<Button>(PreparationSceneUIElements.DecreaseSquadCountButton).RemoveManipulator(_decreaseSquadCountClickable);
        GetElement<Button>(PreparationSceneUIElements.IncreaseSquadCountButton).RemoveManipulator(_increaseSquadCountClickable);
    }

    override protected void SubscriveToGameEvents() { }

    override protected void UnsubscribeFromGameEvents() { }

    private void InitializeHeroCards()
    {
        foreach (HeroCard card in _heroCards)
            card.Dispose();

        _heroCards.Clear();

        VisualElement content = GetElement<VisualElement>(PreparationSceneUIElements.HeroPanel).Q<VisualElement>("Content");
        content.Query<VisualElement>(className: UnitCardWidget.BlockClassName).ForEach(cardElement =>
        {
            if (cardElement == null)
                return;

            _heroCards.Add(new HeroCard(this, cardElement));
        });
    }

    private void InitializeSquadCards()
    {
        foreach (SquadCard card in _squadCards)
            card.Dispose();

        _squadCards.Clear();

        VisualElement content = GetElement<VisualElement>(PreparationSceneUIElements.SquadsPanel).Q<VisualElement>("Content");
        content?.Query<VisualElement>(name: "SquadsContainer").ForEach(cardElement =>
        {
            if (cardElement == null)
                return;

            _squadCards.Add(new SquadCard(this, cardElement));
        });
    }

    private void InitializeSquadSlots()
    {
        foreach (SquadSlot slot in _squadSlots)
            slot.Dispose();

        _squadSlots.Clear();


        int index = 0;
        GetElement<VisualElement>(PreparationSceneUIElements.SquadsList)
            .Query<VisualElement>(className: PreparationSceneClassNames.SquadItem)
            .ForEach(slotElement =>
                {
                    SquadSlot slot = new(this, slotElement, index, index == 0);
                    _squadSlots.Add(slot);
                    index++;
                });

        SetActiveSquadSlot(GetFirstAvailableSlot());
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
        if (_squadSlots.Count == 0)
        {
            UpdateSquadCardFromActiveSlot();
            UpdateSquadCountControls();
            return;
        }

        bool hasDefinitions = _squadDefinitions.Length > 0;
        foreach (SquadSlot slot in _squadSlots)
        {
            if (slot.IsHeroSlot)
                continue;

            if (!hasDefinitions)
            {
                slot.Clear();
                continue;
            }

            if (!slot.IsEmpty && (slot.DefinitionIndex < 0 || slot.DefinitionIndex >= _squadDefinitions.Length))
                slot.Clear();
        }

        if (hasDefinitions)
            PopulateDefaultSquadAssignments();

        RecalculateTotalSquadUnits();
        UpdateTotalUnitCount();
        EnsureActiveSquadSlot();
        UpdateSquadCardFromActiveSlot();
        UpdateSquadCountControls();
        UpdateSlotStates();
    }

    private void ChangeSquadSelection(SquadCard card, int direction)
    {
        if (card == null || _squadDefinitions.Length == 0)
            return;

        if (_activeSquadSlot == null)
            return;

        int currentIndex = _activeSquadSlot.DefinitionIndex;
        int newIndex = currentIndex >= 0
            ? WrapIndex(currentIndex + direction, _squadDefinitions.Length)
            : (direction >= 0 ? 0 : _squadDefinitions.Length - 1);

        TryAssignSquadToSlot(_activeSquadSlot, newIndex);
    }

    private void UpdateSquadCardFromActiveSlot()
    {
        foreach (SquadCard card in _squadCards)
            UpdateSquadCard(card, _activeSquadSlot);
    }

    private void UpdateSquadCard(SquadCard card, SquadSlot slot)
    {
        if (card == null)
            return;

        UnitSO squad = slot?.Squad;
        IReadOnlyList<UnitCardStatField> stats = squad != null ? SquadStatFields : Array.Empty<UnitCardStatField>();
        int definitionIndex = squad != null ? slot.DefinitionIndex : -1;
        int count = squad != null ? slot.Count : DefaultSquadUnitCount;
        card.UpdateContent(squad, definitionIndex, count, stats);
    }

    private void UpdateSquadCountControls()
    {
        int displayCount = DefaultSquadUnitCount;
        bool canDecrease = false;
        bool canIncrease = false;

        if (_activeSquadSlot != null && !_activeSquadSlot.IsEmpty)
        {
            displayCount = _activeSquadSlot.Count;
            canDecrease = _activeSquadSlot.Count > MinSquadUnitCount;
            int available = GetAvailableUnitsForSlot(_activeSquadSlot);
            canIncrease = _activeSquadSlot.Count < available;
        }

        GetElement<Label>(PreparationSceneUIElements.SquadCountLabel).text = displayCount.ToString();
        GetElement<VisualElement>(PreparationSceneUIElements.DecreaseSquadCountButton).SetEnabled(canDecrease);
        GetElement<VisualElement>(PreparationSceneUIElements.IncreaseSquadCountButton).SetEnabled(canIncrease);
    }

    private void UpdateSlotStates()
    {
        bool atLimit = _totalSquadUnits >= MaxSquadUnitsLimit;
        bool needsActiveUpdate = _activeSquadSlot == null;
        foreach (SquadSlot slot in _squadSlots)
        {
            if (slot.IsHeroSlot)
                continue;

            bool shouldLock = atLimit && slot.IsEmpty;
            slot.SetLocked(shouldLock);
            if (slot == _activeSquadSlot && shouldLock)
            {
                _activeSquadSlot = null;
                needsActiveUpdate = true;
            }
            slot.SetActive(slot == _activeSquadSlot);
        }

        if (needsActiveUpdate)
            EnsureActiveSquadSlot();
    }

    private void HandleSquadSlotClick(SquadSlot slot, ClickEvent evt)
    {
        if (slot == null || slot.IsHeroSlot)
            return;

        if (evt != null && evt.clickCount >= 2)
        {
            RemoveSquadFromSlot(slot);
            return;
        }

        if (slot.IsLocked)
            return;

        SetActiveSquadSlot(slot);

        if (slot.IsEmpty)
        {
            int defaultDefinitionIndex = GetDefaultDefinitionIndexForSlot(slot);
            if (defaultDefinitionIndex >= 0)
                TryAssignSquadToSlot(slot, defaultDefinitionIndex);
        }
    }

    private void RemoveSquadFromSlot(SquadSlot slot)
    {
        if (slot == null || slot.IsHeroSlot)
            return;

        if (slot.IsEmpty)
            return;

        if (!HasOtherOccupiedSlots(slot))
            return;

        bool wasActive = slot == _activeSquadSlot;
        slot.Clear();
        if (wasActive)
            _activeSquadSlot = null;

        RecalculateTotalSquadUnits();
        UpdateTotalUnitCount();
        UpdateSlotStates();

        SquadSlot nextActive = FindNearestOccupiedSlot(slot);
        SetActiveSquadSlot(nextActive);
        UpdateSquadCardFromActiveSlot();
        UpdateSquadCountControls();
        UpdateSlotStates();
    }

    private bool HasOtherOccupiedSlots(SquadSlot excludedSlot)
    {
        foreach (SquadSlot candidate in _squadSlots)
        {
            if (candidate == null || candidate.IsHeroSlot || candidate.IsEmpty || candidate == excludedSlot)
                continue;

            return true;
        }

        return false;
    }

    private SquadSlot FindNearestOccupiedSlot(SquadSlot slot)
    {
        if (slot == null)
            return null;

        for (int i = slot.Index - 1; i >= 0; i--)
        {
            SquadSlot candidate = _squadSlots[i];
            if (candidate == null || candidate.IsHeroSlot || candidate.IsEmpty)
                continue;

            return candidate;
        }

        for (int i = slot.Index + 1; i < _squadSlots.Count; i++)
        {
            SquadSlot candidate = _squadSlots[i];
            if (candidate == null || candidate.IsHeroSlot || candidate.IsEmpty)
                continue;

            return candidate;
        }

        return null;
    }

    private void SetActiveSquadSlot(SquadSlot slot)
    {
        if (slot != null && (slot.IsHeroSlot || slot.IsLocked))
            return;

        if (_activeSquadSlot == slot)
        {
            _activeSquadSlot?.SetActive(true);
            return;
        }

        _activeSquadSlot?.SetActive(false);
        _activeSquadSlot = slot;
        _activeSquadSlot?.SetActive(true);
        UpdateSquadCardFromActiveSlot();
        UpdateSquadCountControls();
    }

    private void EnsureActiveSquadSlot()
    {
        if (_activeSquadSlot != null && !_activeSquadSlot.IsHeroSlot && !_activeSquadSlot.IsLocked)
            return;

        SetActiveSquadSlot(GetFirstAvailableSlot());
    }

    private SquadSlot GetFirstAvailableSlot()
    {
        foreach (SquadSlot slot in _squadSlots)
        {
            if (slot == null || slot.IsHeroSlot || slot.IsLocked)
                continue;

            return slot;
        }

        return null;
    }

    private void ChangeActiveSlotCount(int delta)
    {
        if (_activeSquadSlot == null || _activeSquadSlot.IsEmpty)
            return;

        if (delta == 0)
            return;

        int maxForSlot = GetAvailableUnitsForSlot(_activeSquadSlot);
        int newCount = Mathf.Clamp(_activeSquadSlot.Count + delta, MinSquadUnitCount, maxForSlot);
        if (newCount == _activeSquadSlot.Count)
            return;

        _activeSquadSlot.UpdateCount(newCount);
        RecalculateTotalSquadUnits();
        UpdateTotalUnitCount();
        UpdateSquadCardFromActiveSlot();
        UpdateSquadCountControls();
        UpdateSlotStates();
    }

    private void UpdateTotalUnitCount()
    {
        var availableUnitsCount = MaxSquadUnitsLimit - _totalSquadUnits;
        GetElement<Label>(PreparationSceneUIElements.TotalUnitsLabel).text =
            $"Всего юнитов: {_totalSquadUnits}, доступно еще: {availableUnitsCount}";
    }

    private int GetAvailableUnitsForSlot(SquadSlot slot)
    {
        if (slot == null)
            return MaxSquadUnitsLimit;

        int totalWithoutSlot = GetTotalUnitsExcludingSlot(slot);
        return Mathf.Max(0, MaxSquadUnitsLimit - totalWithoutSlot);
    }

    private int GetTotalUnitsExcludingSlot(SquadSlot slot)
    {
        int total = 0;
        foreach (SquadSlot other in _squadSlots)
        {
            if (other == null || other.IsHeroSlot || other.IsEmpty || other == slot)
                continue;

            total += other.Count;
        }

        return total;
    }

    private bool TryAssignSquadToSlot(SquadSlot slot, int definitionIndex)
    {
        if (slot == null || slot.IsHeroSlot)
            return false;

        if (definitionIndex < 0 || definitionIndex >= _squadDefinitions.Length)
            return false;

        UnitSO squad = _squadDefinitions[definitionIndex];
        if (squad == null)
            return false;

        int available = GetAvailableUnitsForSlot(slot);
        int desiredCount = slot.IsEmpty ? DefaultSquadUnitCount : slot.Count;
        desiredCount = Mathf.Clamp(desiredCount, MinSquadUnitCount, available);

        if (available < MinSquadUnitCount && slot.IsEmpty)
            return false;

        slot.UpdateSquad(squad, definitionIndex, desiredCount);
        RecalculateTotalSquadUnits();
        UpdateTotalUnitCount();
        UpdateSquadCardFromActiveSlot();
        UpdateSquadCountControls();
        UpdateSlotStates();
        return true;
    }

    private void PopulateDefaultSquadAssignments()
    {
        foreach (SquadSlot slot in _squadSlots)
        {
            if (slot == null || slot.IsHeroSlot || slot.IsLocked || !slot.IsEmpty)
                continue;

            int defaultDefinitionIndex = GetDefaultDefinitionIndexForSlot(slot);
            if (defaultDefinitionIndex < 0)
                continue;

            TryAssignSquadToSlot(slot, defaultDefinitionIndex);
        }
    }

    private int GetDefaultDefinitionIndexForSlot(SquadSlot slot)
    {
        if (slot == null || slot.IsHeroSlot)
            return -1;

        return 0;
    }

    private void RecalculateTotalSquadUnits()
    {
        int total = 0;
        foreach (SquadSlot slot in _squadSlots)
        {
            if (slot == null || slot.IsHeroSlot || slot.IsEmpty)
                continue;

            total += slot.Count;
        }

        _totalSquadUnits = Mathf.Clamp(total, 0, MaxSquadUnitsLimit);
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

    private void SelectHeroByDirection(int direction)
    {
        if (_heroDefinitions.Length == 0)
            return;

        int targetIndex;
        if (_selectedHeroIndex < 0)
            targetIndex = direction >= 0 ? 0 : _heroDefinitions.Length - 1;
        else
            targetIndex = WrapIndex(_selectedHeroIndex + direction, _heroDefinitions.Length);

        SelectHero(targetIndex);
    }

    private void UpdateHeroSelection()
    {
        foreach (HeroCard card in _heroCards)
        {
            bool isSelected = card.DefinitionIndex >= 0 && card.DefinitionIndex == _selectedHeroIndex;
            card.UpdateSelected(isSelected);
        }

        UpdateHeroSlot();
    }

    private void UpdateHeroSlot()
    {
        if (_squadSlots.Count == 0)
            return;

        SquadSlot heroSlot = _squadSlots[0];
        if (heroSlot == null || !heroSlot.IsHeroSlot)
            return;

        heroSlot.UpdateHero(GetSelectedHero());
    }

    private void ShowHeroSelection()
    {
        SetPanelActive(GetElement<VisualElement>(PreparationSceneUIElements.SquadsPanel), false);
        SetPanelActive(GetElement<VisualElement>(PreparationSceneUIElements.HeroPanel), true);
        _isHeroSelectionVisible = true;
    }

    private void ShowSquadSelection()
    {
        SetPanelActive(GetElement<VisualElement>(PreparationSceneUIElements.HeroPanel), false);
        SetPanelActive(GetElement<VisualElement>(PreparationSceneUIElements.SquadsPanel), true);
        _isHeroSelectionVisible = false;
    }

    private void SubscribeToHeroNavigationInput()
    {
        _selectPrevHeroAction = _inputService.Actions.FindAction("SelectPrevHero", throwIfNotFound: false);
        if (_selectPrevHeroAction != null)
            _selectPrevHeroAction.performed += HandleSelectPrevHero;
        else
            Debug.LogWarning("Menu action 'Prev' was not found for hero navigation", this);

        _selectNextHeroAction = _inputService.Actions.FindAction("SelectNextHero", throwIfNotFound: false);
        if (_selectNextHeroAction != null)
            _selectNextHeroAction.performed += HandleSelectNextHero;
        else
            Debug.LogWarning("Menu action 'Next' was not found for hero navigation", this);
    }

    private void UnsubscribeFromHeroNavigationInput()
    {
        if (_selectPrevHeroAction != null)
        {
            _selectPrevHeroAction.performed -= HandleSelectPrevHero;
            _selectPrevHeroAction = null;
        }

        if (_selectNextHeroAction != null)
        {
            _selectNextHeroAction.performed -= HandleSelectNextHero;
            _selectNextHeroAction = null;
        }
    }

    private void HandleSelectPrevHero(InputAction.CallbackContext _)
    {
        HandleHeroNavigation(-1);
    }

    private void HandleSelectNextHero(InputAction.CallbackContext _)
    {
        HandleHeroNavigation(1);
    }

    private void HandleHeroNavigation(int direction)
    {
        if (!_isHeroSelectionVisible)
            return;

        SelectHeroByDirection(direction);
    }

    private void HandleGoToSquadsSelection()
    {
        ShowSquadSelection();
    }

    private void HandleGoToHeroSelection()
    {
        ShowHeroSelection();
    }

    private void HandleDiveIntoCaveClicked()
    {
        UnitSO selectedHero = GetSelectedHero();
        List<SquadSelection> selectedSquads = GetSelectedSquads();
        _sceneEventBusService.Publish(new RequestDiveIntoCave(selectedHero, selectedSquads));
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

        foreach (SquadSlot slot in _squadSlots)
        {
            if (slot == null || slot.IsHeroSlot || slot.IsEmpty)
                continue;

            UnitSO squad = slot.Squad;
            if (squad == null)
                continue;

            selectedSquads.Add(new SquadSelection(squad, slot.Count));
        }

        return selectedSquads;
    }

    private static void SetPanelActive(VisualElement panel, bool isActive)
    {
        panel.EnableInClassList("panel__active", isActive);
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
        private readonly PreparationSceneUIController _controller;
        private readonly UnitCardWidget _card;
        private readonly Clickable _clickable;

        public HeroCard(PreparationSceneUIController controller, VisualElement root)
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
            IReadOnlySquadModel squad = hero != null ? new SquadModel(hero, Mathf.RoundToInt(DefaultSquadUnitCount)) : null;
            IReadOnlyList<BattleAbilitySO> abilities = hero?.Abilities ?? Array.Empty<BattleAbilitySO>();

            UnitCardRenderData data = new(squad, statFields, abilities, Array.Empty<BattleEffectSO>(), hero.UnitName);
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

    private sealed class SquadSlot
    {
        private readonly PreparationSceneUIController _controller;
        private readonly VisualElement _root;
        private readonly bool _isHeroSlot;
        private EventCallback<ClickEvent> _clickCallback;
        private Label _countLabel;
        private UnitSO _squad;
        private int _count;
        private bool _isLocked;

        public SquadSlot(PreparationSceneUIController controller, VisualElement root, int index, bool isHeroSlot)
        {
            _controller = controller;
            _root = root;
            Index = index;
            _isHeroSlot = isHeroSlot;

            if (_root != null && _isHeroSlot)
                _root.AddToClassList(PreparationSceneClassNames.SquadSlotHero);

            if (!_isHeroSlot && _root != null)
            {
                _countLabel = _root.Q<Label>(className: PreparationSceneClassNames.SquadSlotCount);
                if (_countLabel == null)
                {
                    _countLabel = new Label();
                    _countLabel.AddToClassList(PreparationSceneClassNames.SquadSlotCount);
                    _root.Add(_countLabel);
                }

                _clickCallback = evt => _controller.HandleSquadSlotClick(this, evt);
                _root.RegisterCallback(_clickCallback);
            }

            if (_isHeroSlot && _root != null)
                _root.SetEnabled(false);
        }

        public int Index { get; }
        public bool IsHeroSlot => _isHeroSlot;
        public bool IsLocked => _isLocked;
        public bool IsEmpty => _squad == null;
        public UnitSO Squad => _squad;
        public int DefinitionIndex { get; private set; } = -1;
        public int Count => _count;

        public void UpdateHero(UnitSO hero)
        {
            if (!_isHeroSlot)
                return;

            _squad = null;
            DefinitionIndex = -1;
            _count = 0;
            SetBackground(hero?.Icon);
        }

        public void UpdateSquad(UnitSO squad, int definitionIndex, int count)
        {
            if (_isHeroSlot)
                return;

            _squad = squad;
            DefinitionIndex = definitionIndex;
            _count = Mathf.Max(MinSquadUnitCount, count);
            SetBackground(squad?.Icon);
            UpdateCountLabel();
        }

        public void UpdateCount(int count)
        {
            if (_isHeroSlot)
                return;

            _count = Mathf.Max(MinSquadUnitCount, count);
            UpdateCountLabel();
        }

        public void Clear()
        {
            if (_isHeroSlot)
                return;

            _squad = null;
            DefinitionIndex = -1;
            _count = 0;
            SetBackground(null);
            UpdateCountLabel();
        }

        public void SetActive(bool isActive)
        {
            if (_isHeroSlot)
                return;

            _root?.EnableInClassList(PreparationSceneClassNames.SquadSlotActive, isActive);
        }

        public void SetLocked(bool isLocked)
        {
            if (_isHeroSlot)
                return;

            if (_isLocked == isLocked)
                return;

            _isLocked = isLocked;
            _root?.EnableInClassList(PreparationSceneClassNames.SquadSlotLocked, isLocked);
            _root?.SetEnabled(!isLocked);
            if (isLocked)
                SetActive(false);
        }

        public void Dispose()
        {
            if (_root != null && _clickCallback != null)
                _root.UnregisterCallback(_clickCallback);
        }

        private void SetBackground(Sprite icon)
        {
            if (_root == null)
                return;

            if (icon != null)
                _root.style.backgroundImage = new StyleBackground(icon);
            else
                _root.style.backgroundImage = StyleKeyword.Null;
        }

        private void UpdateCountLabel()
        {
            if (_countLabel == null)
                return;

            _countLabel.text = _count > 0 ? _count.ToString() : string.Empty;
        }
    }

    private sealed class SquadCard
    {
        private readonly PreparationSceneUIController _controller;
        private readonly VisualElement _prevButton;
        private readonly VisualElement _nextButton;
        private readonly Clickable _prevClickable;
        private readonly Clickable _nextClickable;
        private readonly UnitCardWidget _card;

        public SquadCard(PreparationSceneUIController controller, VisualElement root)
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

        public void UpdateContent(UnitSO squad, int index, int count, IReadOnlyList<UnitCardStatField> stats)
        {
            SelectedDefinitionIndex = index;
            if (_card == null)
                return;

            IReadOnlyList<UnitCardStatField> statFields = squad != null ? stats : Array.Empty<UnitCardStatField>();
            int effectiveCount = squad != null ? Mathf.Max(MinSquadUnitCount, count) : 0;
            IReadOnlySquadModel squadModel = squad != null ? new SquadModel(squad, effectiveCount) : null;
            IReadOnlyList<BattleAbilitySO> abilities = squad?.Abilities ?? Array.Empty<BattleAbilitySO>();

            string title = squad != null ? squad.UnitName : string.Empty;
            UnitCardRenderData data = new(squadModel, statFields, abilities, Array.Empty<BattleEffectSO>(), title);
            _card.Render(data);
            _card.SetEnabled(squad != null);
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
