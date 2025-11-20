using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UICommon.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

public enum DungeonSceneElements
{
    Root,
    SquadsList,
    DialogContainer,
    DialogLabel,
    SquadInfoCard
}

public sealed class DungeonSceneUIController: BaseUIController<DungeonSceneElements>
{
    private static readonly UnitCardStatField[] DungeonCardStatFields =
    {
        UnitCardStatField.Count,
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

    [SerializeField, Min(0f)]
    private float _dialogSecondsPerCharacter = 0.05f;

    private readonly List<IReadOnlySquadModel> _buffer = new();
    private readonly List<VisualElement> _unitCardAnchors = new();
    private readonly List<IReadOnlySquadModel> _lastRendered = new();
    private readonly List<IReadOnlySquadModel> _orderedSquads = new();
    private readonly Dictionary<IReadOnlySquadModel, SquadEntry> _squadEntries = new();

    private readonly StringBuilder _dialogBuilder = new();

    private Coroutine _dialogRoutine;
    private UnitCardWidget _squadInfoCardWidget;
    private IReadOnlySquadModel _displayedSquad;

    public float DialogSecondsPerCharacter => _dialogSecondsPerCharacter;

    protected override void RegisterUIElements()
    {
        var root = _uiDocument.rootVisualElement;

        _uiElements[DungeonSceneElements.Root] = root;

        var squadsList = root.Q<VisualElement>("SquadsList");
        _uiElements[DungeonSceneElements.SquadsList] = squadsList;

        var dialogContainer = root.Q<VisualElement>("Dialog");
        _uiElements[DungeonSceneElements.DialogContainer] = dialogContainer;

        Label dialogLabel = null;
        if (dialogContainer != null)
        {
            dialogLabel = dialogContainer.Q<Label>("DialogLabel");
            if (dialogLabel == null)
            {
                dialogLabel = new Label { name = "DialogLabel" };
                dialogLabel.AddToClassList("dialog-text");
                dialogContainer.Add(dialogLabel);
            }
        }
        _uiElements[DungeonSceneElements.DialogLabel] = dialogLabel;

        var squadInfoCard = root.Q<VisualElement>("SquadInfoCard");
        _uiElements[DungeonSceneElements.SquadInfoCard] = squadInfoCard;

        _squadInfoCardWidget = squadInfoCard != null ? new UnitCardWidget(squadInfoCard) : null;

        if (dialogContainer != null)
            SetDialogVisibility(false);
    }

    protected override void SubcribeToUIEvents() { }

    protected override void UnsubscriveFromUIEvents() { }

    protected override void SubscriveToGameEvents() { }

    protected override void UnsubscribeFromGameEvents() { }

    protected override void DetachFromPanel()
    {
        if (!_isAttached)
            return;

        HideDialog();
        HideSquadInfoCard();
        ClearTrackedSquads();

        _squadInfoCardWidget?.RegisterAnchors(null);
        _squadInfoCardWidget = null;

        base.DetachFromPanel();
    }

    public void RenderSquads(IEnumerable<IReadOnlySquadModel> squads)
    {
        if (!_isAttached)
            return;

        _buffer.Clear();

        if (squads != null)
        {
            foreach (var squad in squads)
            {
                if (squad == null || squad.IsEmpty)
                    continue;

                if (_buffer.Contains(squad))
                    continue;

                _buffer.Add(squad);
            }
        }

        var squadsList = GetElement<VisualElement>(DungeonSceneElements.SquadsList);
        for (int i = _orderedSquads.Count - 1; i >= 0; i--)
        {
            var tracked = _orderedSquads[i];
            if (!_buffer.Contains(tracked))
                RemoveSquad(tracked);
        }

        for (int i = 0; i < _buffer.Count; i++)
        {
            var squad = _buffer[i];
            if (!_squadEntries.TryGetValue(squad, out var entry))
            {
                entry = CreateSquadEntry(squad);
                _squadEntries[squad] = entry;
                squad.Changed += HandleSquadChanged;
            }

            UpdateSquadEntry(entry, squad);
        }

        squadsList.Clear();
        _orderedSquads.Clear();

        for (int i = 0; i < _buffer.Count; i++)
        {
            var squad = _buffer[i];
            if (_squadEntries.TryGetValue(squad, out var entry))
            {
                squadsList.Add(entry.Root);
                _orderedSquads.Add(squad);
            }
        }

        _lastRendered.Clear();
        _lastRendered.AddRange(_orderedSquads);
        UpdateSquadCardAnchors();
    }

    public void RenderDialog(string text, float? overrideSecondsPerCharacter = null)
    {
        var dialogContainer = GetElement<VisualElement>(DungeonSceneElements.DialogContainer);
        var dialogLabel = GetElement<Label>(DungeonSceneElements.DialogLabel);

        if (dialogContainer == null || dialogLabel == null)
            return;

        StopDialogRoutine();
        SetDialogVisibility(true);

        var message = text ?? string.Empty;
        var secondsPerCharacter = overrideSecondsPerCharacter.HasValue
            ? Mathf.Max(0f, overrideSecondsPerCharacter.Value)
            : _dialogSecondsPerCharacter;

        _dialogRoutine = StartCoroutine(TypeDialogRoutine(message, secondsPerCharacter));
    }

    public void HideDialog()
    {
        var dialogContainer = GetElement<VisualElement>(DungeonSceneElements.DialogContainer);
        var dialogLabel = GetElement<Label>(DungeonSceneElements.DialogLabel);

        if (dialogContainer == null || dialogLabel == null)
            return;

        StopDialogRoutine();
        _dialogBuilder.Clear();
        dialogLabel.text = string.Empty;
        SetDialogVisibility(false);
    }

    private IEnumerator TypeDialogRoutine(string message, float secondsPerCharacter)
    {
        var dialogLabel = GetElement<Label>(DungeonSceneElements.DialogLabel);
        if (dialogLabel == null)
        {
            _dialogRoutine = null;
            yield break;
        }

        message ??= string.Empty;
        dialogLabel.text = string.Empty;
        _dialogBuilder.Clear();

        if (message.Length == 0)
        {
            _dialogRoutine = null;
            yield break;
        }

        if (secondsPerCharacter <= 0f)
        {
            dialogLabel.text = message;
            _dialogRoutine = null;
            yield break;
        }

        for (int i = 0; i < message.Length; i++)
        {
            _dialogBuilder.Append(message[i]);
            dialogLabel.text = _dialogBuilder.ToString();
            yield return new WaitForSeconds(secondsPerCharacter);
        }

        _dialogRoutine = null;
    }

    private void StopDialogRoutine()
    {
        if (_dialogRoutine != null)
        {
            StopCoroutine(_dialogRoutine);
            _dialogRoutine = null;
        }
    }

    private void SetDialogVisibility(bool isVisible)
    {
        var dialogContainer = GetElement<VisualElement>(DungeonSceneElements.DialogContainer);
        if (dialogContainer == null)
            return;

        dialogContainer.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        dialogContainer.visible = isVisible;
    }

    private void HandleSquadChanged(IReadOnlySquadModel squad)
    {
        if (squad == null)
            return;

        if (_squadEntries.TryGetValue(squad, out var entry))
        {
            UpdateSquadEntry(entry, squad);
            if (ReferenceEquals(_displayedSquad, squad))
                UpdateSquadInfoCardContent(squad);
        }
    }

    private void RemoveSquad(IReadOnlySquadModel squad)
    {
        if (squad == null)
            return;

        if (_squadEntries.TryGetValue(squad, out var entry))
        {
            squad.Changed -= HandleSquadChanged;
            _squadEntries.Remove(squad);
            entry.Root.RemoveFromHierarchy();
            entry.Dispose();
            if (ReferenceEquals(_displayedSquad, squad))
                HideSquadInfoCard();
        }
    }

    private void ClearTrackedSquads()
    {
        foreach (var kvp in _squadEntries)
        {
            kvp.Key.Changed -= HandleSquadChanged;
            kvp.Value.Dispose();
        }

        _squadEntries.Clear();
        _orderedSquads.Clear();
        _buffer.Clear();

        var squadsList = GetElement<VisualElement>(DungeonSceneElements.SquadsList);
        squadsList?.Clear();

        HideSquadInfoCard();
        _unitCardAnchors.Clear();
        _squadInfoCardWidget?.RegisterAnchors(null);
    }

    private SquadEntry CreateSquadEntry(IReadOnlySquadModel squad)
    {
        var root = new VisualElement
        {
            name = $"Squad_{squad?.UnitName ?? "Unknown"}"
        };

        root.AddToClassList("squad-entry");

        var icon = new VisualElement
        {
            name = "SquadIcon"
        };

        icon.AddToClassList("squad-entry__icon");

        var countLabel = new Label
        {
            name = "SquadCount"
        };

        countLabel.AddToClassList("squad-entry__count");

        root.Add(icon);
        root.Add(countLabel);

        return new SquadEntry(this, root, icon, countLabel);
    }

    private static void UpdateSquadEntry(SquadEntry entry, IReadOnlySquadModel squad)
    {
        entry?.SetSquad(squad);

        if (entry.Icon != null)
        {
            if (squad != null && squad.Icon != null)
            {
                entry.Icon.style.backgroundImage = new StyleBackground(squad.Icon);
                entry.Icon.visible = true;
            }
            else
            {
                entry.Icon.style.backgroundImage = new StyleBackground();
                entry.Icon.visible = false;
            }
        }

        if (entry.CountLabel != null)
        {
            entry.CountLabel.text = squad != null && !squad.IsEmpty
                ? $"{squad.UnitName} x{squad.Count}"
                : string.Empty;
        }
    }

    private void ShowSquadInfoCard(IReadOnlySquadModel squad)
    {
        if (squad == null || _squadInfoCardWidget == null)
            return;

        _displayedSquad = squad;
        UpdateSquadInfoCardContent(squad);
    }

    private void UpdateSquadInfoCardContent(IReadOnlySquadModel squad)
    {
        if (squad == null || _squadInfoCardWidget == null)
            return;

        IReadOnlyList<BattleAbilitySO> abilities = squad.Abilities ?? Array.Empty<BattleAbilitySO>();
        Dictionary<string, object> fields = new()
        {
            [UnitCardWidget.LevelFieldKey] = UnitCardWidget.FormatLevelText(squad)
        };

        if (TryCreateLevelProgressData(squad, out UnitCardLevelProgressData progressData))
            fields[UnitCardWidget.LevelProgressFieldKey] = progressData;

        UnitCardRenderData data = new(
            squad,
            DungeonCardStatFields,
            abilities,
            Array.Empty<BattleEffectSO>(),
            squad.UnitName,
            null,
            fields);

        _squadInfoCardWidget.Render(data);
    }

    private void HideSquadInfoCard()
    {
        _displayedSquad = null;
        _squadInfoCardWidget?.SetVisible(false);
    }

    private void UpdateSquadCardAnchors()
    {
        if (_squadInfoCardWidget == null)
        {
            _unitCardAnchors.Clear();
            return;
        }

        _unitCardAnchors.Clear();

        foreach (IReadOnlySquadModel squad in _orderedSquads)
        {
            if (squad == null)
                continue;

            if (_squadEntries.TryGetValue(squad, out var entry) && entry?.Root != null)
                _unitCardAnchors.Add(entry.Root);
        }

        _squadInfoCardWidget.RegisterAnchors(_unitCardAnchors);
    }

    private static bool TryCreateLevelProgressData(IReadOnlySquadModel squad, out UnitCardLevelProgressData progressData)
    {
        progressData = default;

        if (squad?.Definition == null)
            return false;

        int level = Mathf.Max(1, squad.Level);
        UnitLevelExpFunction expFunction = squad.Definition.LevelExpFunction;
        float currentLevelExp = expFunction.GetExperienceForLevel(level);
        float nextLevelExp = expFunction.GetExperienceForLevel(level + 1);
        float totalRequired = Mathf.Max(0.0001f, nextLevelExp - currentLevelExp);
        float gained = Mathf.Clamp(squad.Experience - currentLevelExp, 0f, totalRequired);
        float progress = Mathf.Clamp01(gained / totalRequired);
        float remaining = Mathf.Max(0f, nextLevelExp - squad.Experience);
        string title = $"До уровня {level + 1}: {Mathf.CeilToInt(remaining)} опыта";

        progressData = new UnitCardLevelProgressData(progress, title);
        return true;
    }

    private sealed class SquadEntry : IDisposable
    {
        private readonly DungeonSceneUIController _owner;
        private readonly EventCallback<PointerEnterEvent> _pointerEnterCallback;

        public SquadEntry(DungeonSceneUIController owner, VisualElement root, VisualElement icon, Label countLabel)
        {
            _owner = owner;
            Root = root;
            Icon = icon;
            CountLabel = countLabel;
            _pointerEnterCallback = HandlePointerEnter;
            Root?.RegisterCallback(_pointerEnterCallback);
        }

        public VisualElement Root { get; }

        public VisualElement Icon { get; }

        public Label CountLabel { get; }

        public IReadOnlySquadModel Squad { get; private set; }

        public void SetSquad(IReadOnlySquadModel squad)
        {
            Squad = squad;
        }

        public void Dispose()
        {
            Root?.UnregisterCallback(_pointerEnterCallback);
        }

        private void HandlePointerEnter(PointerEnterEvent _)
        {
            if (Squad == null)
                return;

            _owner?.ShowSquadInfoCard(Squad);
        }
    }
}
