using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UICommon.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DungeonUIController : MonoBehaviour, ISceneUIController
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField, Min(0f)] private float _dialogSecondsPerCharacter = 0.05f;

    private const string SquadInfoCardElementName = "SquadInfoCard";
    private const string CardHiddenClassName = "dungeon-card--hidden";
    private const string CardVisibleClassName = "dungeon-card--visible";

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

    private VisualElement _root;
    private VisualElement _squadsList;
    private VisualElement _dialogContainer;
    private Label _dialogLabel;
    private VisualElement _squadInfoCard;
    private UnitCardWidget _squadInfoCardWidget;
    private IReadOnlySquadModel _displayedSquad;

    private readonly Dictionary<IReadOnlySquadModel, SquadEntry> _squadEntries = new();
    private readonly List<IReadOnlySquadModel> _orderedSquads = new();
    private readonly List<IReadOnlySquadModel> _buffer = new();
    private readonly List<IReadOnlySquadModel> _lastRenderedSquads = new();

    private Coroutine _dialogRoutine;
    private readonly StringBuilder _dialogBuilder = new();
    private bool _isAttached;

    public float DialogSecondsPerCharacter => _dialogSecondsPerCharacter;

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

        if (_uiDocument?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }
    }

    public void AttachToPanel(UIDocument document)
    {
        if (document == null)
        {
            return;
        }

        if (_isAttached)
        {
            DetachFromPanel();
        }

        _uiDocument = document;
        _root = document.rootVisualElement;
        _squadsList = _root?.Q<VisualElement>("SquadsList");
        if (_squadsList != null)
        {
            _squadsList.UnregisterCallback<PointerLeaveEvent>(HandleSquadsListPointerLeave);
            _squadsList.RegisterCallback<PointerLeaveEvent>(HandleSquadsListPointerLeave);
        }
        _dialogContainer = _root?.Q<VisualElement>("Dialog");
        _squadInfoCard = _root?.Q<VisualElement>(SquadInfoCardElementName);
        _squadInfoCardWidget = _squadInfoCard != null ? new UnitCardWidget(_squadInfoCard) : null;
        HideSquadInfoCard();

        if (_dialogContainer != null)
        {
            _dialogLabel = _dialogContainer.Q<Label>("DialogLabel");
            if (_dialogLabel == null)
            {
                _dialogLabel = new Label { name = "DialogLabel" };
                _dialogLabel.AddToClassList("dialog-text");
                _dialogContainer.Add(_dialogLabel);
            }

            SetDialogVisibility(false);
        }
        else
        {
            _dialogLabel = null;
        }

        _isAttached = true;

        RenderSquads(_lastRenderedSquads);
    }

    public void DetachFromPanel()
    {
        if (!_isAttached)
        {
            return;
        }

        HideDialog();
        HideSquadInfoCard();
        ClearTrackedSquads();

        if (_squadsList != null)
        {
            _squadsList.UnregisterCallback<PointerLeaveEvent>(HandleSquadsListPointerLeave);
        }

        _dialogContainer = null;
        _dialogLabel = null;
        _squadInfoCardWidget = null;
        _squadInfoCard = null;
        _displayedSquad = null;
        _squadsList = null;
        _root = null;
        _isAttached = false;
    }

    public void RenderSquads(IEnumerable<IReadOnlySquadModel> squads)
    {
        _buffer.Clear();

        if (squads != null)
        {
            foreach (var squad in squads)
            {
                if (squad == null || squad.IsEmpty)
                {
                    continue;
                }

                if (_buffer.Contains(squad))
                {
                    continue;
                }

                _buffer.Add(squad);
            }
        }

        if (_squadsList == null)
        {
            _lastRenderedSquads.Clear();
            _lastRenderedSquads.AddRange(_buffer);
            HideSquadInfoCard();
            if (_isAttached)
            {
                Debug.LogWarning($"[{nameof(DungeonUIController)}.{nameof(RenderSquads)}] SquadsList element was not found.");
            }
            return;
        }

        for (int i = _orderedSquads.Count - 1; i >= 0; i--)
        {
            var tracked = _orderedSquads[i];
            if (!_buffer.Contains(tracked))
            {
                RemoveSquad(tracked);
            }
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

        _squadsList.Clear();
        _orderedSquads.Clear();

        for (int i = 0; i < _buffer.Count; i++)
        {
            var squad = _buffer[i];
            if (_squadEntries.TryGetValue(squad, out var entry))
            {
                _squadsList.Add(entry.Root);
                _orderedSquads.Add(squad);
            }
        }

        _lastRenderedSquads.Clear();
        _lastRenderedSquads.AddRange(_orderedSquads);
    }

    public void RenderDialog(string text, float? overrideSecondsPerCharacter = null)
    {
        if (_dialogContainer == null || _dialogLabel == null)
        {
            Debug.LogWarning($"[{nameof(DungeonUIController)}.{nameof(RenderDialog)}] Dialog container or label was not found.");
            return;
        }

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
        if (_dialogContainer == null || _dialogLabel == null)
        {
            return;
        }

        StopDialogRoutine();
        _dialogBuilder.Clear();
        _dialogLabel.text = string.Empty;
        SetDialogVisibility(false);
    }

    private IEnumerator TypeDialogRoutine(string message, float secondsPerCharacter)
    {
        _dialogLabel.text = string.Empty;
        _dialogBuilder.Clear();

        if (secondsPerCharacter <= 0f)
        {
            _dialogLabel.text = message;
            _dialogRoutine = null;
            yield break;
        }

        for (int i = 0; i < message.Length; i++)
        {
            _dialogBuilder.Append(message[i]);
            _dialogLabel.text = _dialogBuilder.ToString();
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
        if (_dialogContainer == null)
        {
            return;
        }

        _dialogContainer.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        _dialogContainer.visible = isVisible;
    }

    private void TryRegisterLifecycleCallbacks()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        if (_uiDocument?.rootVisualElement is { } root)
        {
            root.UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
            root.RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

            if (!_isAttached && root.panel != null)
            {
                AttachToPanel(_uiDocument);
            }
        }
    }

    private void HandleAttachToPanel(AttachToPanelEvent _)
    {
        if (!_isAttached)
        {
            AttachToPanel(_uiDocument);
        }
    }

    private void HandleDetachFromPanel(DetachFromPanelEvent _)
    {
        DetachFromPanel();
    }

    private void HandleSquadChanged(IReadOnlySquadModel squad)
    {
        if (squad == null)
        {
            return;
        }

        if (_squadEntries.TryGetValue(squad, out var entry))
        {
            UpdateSquadEntry(entry, squad);
            if (ReferenceEquals(_displayedSquad, squad))
            {
                UpdateSquadInfoCardContent(squad);
            }
        }
    }

    private void RemoveSquad(IReadOnlySquadModel squad)
    {
        if (squad == null)
        {
            return;
        }

        if (_squadEntries.TryGetValue(squad, out var entry))
        {
            squad.Changed -= HandleSquadChanged;
            _squadEntries.Remove(squad);
            entry.Root.RemoveFromHierarchy();
            entry.Dispose();
            if (ReferenceEquals(_displayedSquad, squad))
            {
                HideSquadInfoCard();
            }
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
        _squadsList?.Clear();
        HideSquadInfoCard();
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

    private void HandleSquadsListPointerLeave(PointerLeaveEvent _)
    {
        HideSquadInfoCard();
    }

    private void ShowSquadInfoCard(IReadOnlySquadModel squad)
    {
        if (squad == null || _squadInfoCard == null || _squadInfoCardWidget == null)
        {
            return;
        }

        _displayedSquad = squad;
        UpdateSquadInfoCardContent(squad);

        _squadInfoCard.EnableInClassList(CardHiddenClassName, false);
        _squadInfoCard.EnableInClassList(CardVisibleClassName, true);
    }

    private void UpdateSquadInfoCardContent(IReadOnlySquadModel squad)
    {
        if (squad == null || _squadInfoCardWidget == null)
        {
            return;
        }

        IReadOnlyList<BattleAbilitySO> abilities = squad.Abilities ?? Array.Empty<BattleAbilitySO>();
        UnitCardRenderData data = new(squad, DungeonCardStatFields, abilities, Array.Empty<BattleEffectSO>(), squad.UnitName);
        _squadInfoCardWidget.Render(data);
    }

    private void HideSquadInfoCard()
    {
        _displayedSquad = null;

        if (_squadInfoCard == null)
        {
            return;
        }

        _squadInfoCard.EnableInClassList(CardVisibleClassName, false);
        _squadInfoCard.EnableInClassList(CardHiddenClassName, true);
    }

    private sealed class SquadEntry : IDisposable
    {
        private readonly DungeonUIController _owner;
        private readonly EventCallback<PointerEnterEvent> _pointerEnterCallback;

        public SquadEntry(DungeonUIController owner, VisualElement root, VisualElement icon, Label countLabel)
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
            {
                return;
            }

            _owner?.ShowSquadInfoCard(Squad);
        }
    }
}
