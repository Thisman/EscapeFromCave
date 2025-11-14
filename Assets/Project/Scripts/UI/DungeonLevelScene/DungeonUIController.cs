using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DungeonUIController : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField, Min(0f)] private float _dialogSecondsPerCharacter = 0.05f;
    [SerializeField, Min(0f)] private float _iconSize = 64f;

    private VisualElement _root;
    private VisualElement _squadsList;
    private VisualElement _dialogContainer;
    private Label _dialogLabel;

    private readonly Dictionary<IReadOnlySquadModel, SquadEntry> _squadEntries = new();
    private readonly List<IReadOnlySquadModel> _orderedSquads = new();
    private readonly List<IReadOnlySquadModel> _buffer = new();

    private Coroutine _dialogRoutine;
    private readonly StringBuilder _dialogBuilder = new();

    public float DialogSecondsPerCharacter => _dialogSecondsPerCharacter;

    private void Awake()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
        _squadsList = _root?.Q<VisualElement>("SquadsList");
        _dialogContainer = _root?.Q<VisualElement>("Dialog");

        if (_dialogContainer != null)
        {
            _dialogLabel = _dialogContainer.Q<Label>("DialogLabel");
            if (_dialogLabel == null)
            {
                _dialogLabel = new Label { name = "DialogLabel" };
                _dialogLabel.style.whiteSpace = WhiteSpace.Normal;
                _dialogLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                _dialogLabel.style.flexGrow = 1f;
                _dialogContainer.Add(_dialogLabel);
            }
        }
    }

    private void OnDisable()
    {
        ClearTrackedSquads();
        StopDialogRoutine();
    }

    private void OnDestroy()
    {
        ClearTrackedSquads();
        StopDialogRoutine();
    }

    public void RenderSquads(IEnumerable<IReadOnlySquadModel> squads)
    {
        if (_squadsList == null)
        {
            Debug.LogWarning($"[{nameof(DungeonUIController)}.{nameof(RenderSquads)}] SquadsList element was not found.");
            return;
        }

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
    }

    public void RenderDialog(string text, float? overrideSecondsPerCharacter = null)
    {
        if (_dialogContainer == null || _dialogLabel == null)
        {
            Debug.LogWarning($"[{nameof(DungeonUIController)}.{nameof(RenderDialog)}] Dialog container or label was not found.");
            return;
        }

        StopDialogRoutine();

        var message = text ?? string.Empty;
        var secondsPerCharacter = overrideSecondsPerCharacter.HasValue
            ? Mathf.Max(0f, overrideSecondsPerCharacter.Value)
            : _dialogSecondsPerCharacter;

        _dialogRoutine = StartCoroutine(TypeDialogRoutine(message, secondsPerCharacter));
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

    private void HandleSquadChanged(IReadOnlySquadModel squad)
    {
        if (squad == null)
        {
            return;
        }

        if (_squadEntries.TryGetValue(squad, out var entry))
        {
            UpdateSquadEntry(entry, squad);
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
        }
    }

    private void ClearTrackedSquads()
    {
        foreach (var kvp in _squadEntries)
        {
            kvp.Key.Changed -= HandleSquadChanged;
        }

        _squadEntries.Clear();
        _orderedSquads.Clear();
        _buffer.Clear();
        _squadsList?.Clear();
    }

    private SquadEntry CreateSquadEntry(IReadOnlySquadModel squad)
    {
        var root = new VisualElement
        {
            name = $"Squad_{squad?.UnitName ?? "Unknown"}"
        };

        root.style.flexDirection = FlexDirection.Row;
        root.style.alignItems = Align.Center;
        root.style.marginBottom = 4f;
        root.style.marginTop = 4f;
        root.style.paddingLeft = 4f;
        root.style.paddingRight = 4f;
        root.style.paddingTop = 2f;
        root.style.paddingBottom = 2f;
        root.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
        root.style.borderBottomWidth = 1f;
        root.style.borderTopWidth = 1f;
        root.style.borderLeftWidth = 1f;
        root.style.borderRightWidth = 1f;
        root.style.borderBottomColor = new Color(1f, 1f, 1f, 0.2f);
        root.style.borderTopColor = new Color(1f, 1f, 1f, 0.2f);
        root.style.borderLeftColor = new Color(1f, 1f, 1f, 0.2f);
        root.style.borderRightColor = new Color(1f, 1f, 1f, 0.2f);

        var icon = new Image
        {
            name = "SquadIcon",
            scaleMode = ScaleMode.ScaleToFit
        };

        icon.style.width = _iconSize;
        icon.style.height = _iconSize;
        icon.style.marginRight = 8f;

        var countLabel = new Label
        {
            name = "SquadCount"
        };

        countLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        countLabel.style.fontSize = 18f;
        countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        root.Add(icon);
        root.Add(countLabel);

        return new SquadEntry(root, icon, countLabel);
    }

    private static void UpdateSquadEntry(SquadEntry entry, IReadOnlySquadModel squad)
    {
        if (entry.Icon != null)
        {
            entry.Icon.sprite = squad?.Icon;
            entry.Icon.visible = squad != null && squad.Icon != null;
        }

        if (entry.CountLabel != null)
        {
            entry.CountLabel.text = squad != null && !squad.IsEmpty
                ? $"{squad.UnitName} x{squad.Count}"
                : string.Empty;
        }
    }

    private sealed class SquadEntry
    {
        public SquadEntry(VisualElement root, Image icon, Label countLabel)
        {
            Root = root;
            Icon = icon;
            CountLabel = countLabel;
        }

        public VisualElement Root { get; }
        public Image Icon { get; }
        public Label CountLabel { get; }
    }
}
