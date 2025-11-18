using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BattleEffectEditorWindow : EditorWindow
{
    private const string WindowUxmlPath = "Assets/Editor/BattleEffect/BattleEffectEditor.uxml";
    private const string EffectsFolderPath = "Assets/Resources/GameData/BattleEffects";

    private readonly List<BattleEffectSO> _allEffects = new();
    private readonly List<BattleEffectSO> _filteredEffects = new();

    private ListView _effectsList;
    private TextField _searchField;
    private Button _createButton;
    private VisualElement _contentContainer;
    private VisualElement _iconDropArea;
    private Image _iconImage;
    private Label _iconHintLabel;
    private TextField _effectNameField;
    private TextField _descriptionField;
    private VisualElement _effectTypeContainer;
    private VisualElement _typeSpecificContainer;
    private PopupField<string> _effectTypeField;
    private EnumField _triggerField;
    private EnumField _tickTriggerField;
    private IntegerField _maxTickField;
    private Label _fileNameLabel;
    private Button _saveButton;

    private BattleEffectSO _selectedEffect;
    private BattleEffectSO _editingEffect;
    private bool _hasUnsavedChanges;
    private bool _isCreatingNewEffect;
    private string _searchFilter = string.Empty;

    private readonly List<EffectTypeOption> _effectTypeOptions = new();

    [MenuItem("Editors/Battle Effect")]
    public static void ShowWindow()
    {
        var window = GetWindow<BattleEffectEditorWindow>();
        window.titleContent = new GUIContent("Effect Editor");
        window.minSize = new Vector2(900, 540);
        window.Show();
    }

    private void OnDisable()
    {
        if (_editingEffect != null)
        {
            DestroyImmediate(_editingEffect);
            _editingEffect = null;
        }
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPath);
        if (visualTree == null)
        {
            rootVisualElement.Add(new Label("BattleEffectEditor.uxml not found"));
            return;
        }

        rootVisualElement.Add(visualTree.CloneTree());

        CacheControls();
        LoadEffectTypeOptions();
        SetupEffectTypeField();
        SetupSidebar();
        SetupContent();
        SetupIconDragAndDrop();

        RefreshEffects();
        UpdateContentState();
        UpdateEffectTypeSelection();
    }

    private void CacheControls()
    {
        _effectsList = rootVisualElement.Q<ListView>("EffectsList");
        _searchField = rootVisualElement.Q<TextField>("SearchField");
        _createButton = rootVisualElement.Q<Button>("CreateButton");
        _contentContainer = rootVisualElement.Q<VisualElement>("ContentContainer");
        _iconDropArea = rootVisualElement.Q<VisualElement>("IconDropArea");
        _iconImage = rootVisualElement.Q<Image>("IconImage");
        _iconHintLabel = _iconDropArea?.Q<Label>();
        _effectNameField = rootVisualElement.Q<TextField>("EffectNameField");
        _descriptionField = rootVisualElement.Q<TextField>("DescriptionField");
        _effectTypeContainer = rootVisualElement.Q<VisualElement>("EffectTypeContainer");
        _typeSpecificContainer = rootVisualElement.Q<VisualElement>("EffectTypeFields");
        _triggerField = rootVisualElement.Q<EnumField>("TriggerField");
        _tickTriggerField = rootVisualElement.Q<EnumField>("TickTriggerField");
        _maxTickField = rootVisualElement.Q<IntegerField>("MaxTickField");
        _fileNameLabel = rootVisualElement.Q<Label>("FileNameLabel");
        _saveButton = rootVisualElement.Q<Button>("SaveButton");
    }

    private void LoadEffectTypeOptions()
    {
        _effectTypeOptions.Clear();
        var types = TypeCache.GetTypesDerivedFrom<BattleEffectSO>();
        foreach (var type in types)
        {
            if (type == null || type.IsAbstract || type.IsGenericType)
                continue;

            if (!typeof(BattleEffectSO).IsAssignableFrom(type))
                continue;

            _effectTypeOptions.Add(new EffectTypeOption(type));
        }

        _effectTypeOptions.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
    }

    private void SetupEffectTypeField()
    {
        if (_effectTypeContainer == null)
            return;

        _effectTypeContainer.Clear();

        if (_effectTypeOptions.Count == 0)
            return;

        var labels = _effectTypeOptions.Select(option => option.DisplayName).ToList();
        _effectTypeField = new PopupField<string>("Тип эффекта", labels, 0);
        _effectTypeField.RegisterValueChangedCallback(OnEffectTypeChanged);
        _effectTypeContainer.Add(_effectTypeField);
    }

    private void RefreshEffectTypeChoices()
    {
        if (_effectTypeField == null)
            return;

        _effectTypeField.choices = _effectTypeOptions.Select(option => option.DisplayName).ToList();
    }

    private EffectTypeOption EnsureEffectTypeOption(Type type)
    {
        if (type == null)
            return null;

        var option = _effectTypeOptions.FirstOrDefault(o => o.Type == type);
        if (option != null)
            return option;

        option = new EffectTypeOption(type);
        _effectTypeOptions.Add(option);
        _effectTypeOptions.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
        RefreshEffectTypeChoices();
        return option;
    }

    private void UpdateEffectTypeSelection()
    {
        if (_effectTypeField == null)
            return;

        if (_editingEffect == null)
        {
            if (_effectTypeField.choices.Count > 0)
            {
                _effectTypeField.SetValueWithoutNotify(_effectTypeField.choices[0]);
            }

            _effectTypeField.SetEnabled(false);
            return;
        }

        var option = EnsureEffectTypeOption(_editingEffect.GetType());
        if (option == null)
            return;

        _effectTypeField.SetValueWithoutNotify(option.DisplayName);
        _effectTypeField.SetEnabled(_isCreatingNewEffect);
    }

    private void SetupSidebar()
    {
        if (_effectsList == null)
            return;

        _effectsList.selectionType = SelectionType.Single;
        _effectsList.fixedItemHeight = 26;
        _effectsList.makeItem = () =>
        {
            var label = new Label();
            label.AddToClassList("effect-editor__item");
            return label;
        };
        _effectsList.bindItem = (element, index) =>
        {
            if (index < 0 || index >= _filteredEffects.Count)
                return;

            if (element is Label label)
            {
                var effect = _filteredEffects[index];
                label.text = effect == null ? "-" : GetEffectFileName(effect);
            }
        };
        _effectsList.selectionChanged += objects =>
        {
            var effect = objects?.OfType<BattleEffectSO>().FirstOrDefault();
            if (effect != _selectedEffect)
            {
                SelectEffect(effect);
            }
        };

        if (_searchField != null)
        {
            _searchField.label = string.Empty;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue?.Trim() ?? string.Empty;
                ApplyFilter();
            });
        }

        if (_createButton != null)
        {
            _createButton.clicked += CreateEffectAsset;
        }
    }

    private void SetupContent()
    {
        if (_effectNameField != null)
        {
            _effectNameField.RegisterValueChangedCallback(evt =>
            {
                if (_editingEffect == null)
                    return;

                _editingEffect.Name = evt.newValue;
                UpdateFileNameLabel();
                MarkDirty();
            });
        }

        if (_descriptionField != null)
        {
            _descriptionField.multiline = true;
            _descriptionField.RegisterValueChangedCallback(evt =>
            {
                if (_editingEffect == null)
                    return;

                _editingEffect.Description = evt.newValue;
                MarkDirty();
            });
        }

        ConfigureEnumField<BattleEffectTrigger>(_triggerField, value => _editingEffect.Trigger = value);
        ConfigureEnumField<BattleEffectTrigger>(_tickTriggerField, value => _editingEffect.TickTrigger = value);
        ConfigureIntegerField(_maxTickField, value => _editingEffect.MaxTick = Mathf.Max(0, value));

        if (_saveButton != null)
        {
            _saveButton.clicked += SaveCurrentEffect;
        }
    }

    private void OnEffectTypeChanged(ChangeEvent<string> evt)
    {
        if (_effectTypeField == null)
            return;

        if (_editingEffect == null)
        {
            UpdateEffectTypeSelection();
            return;
        }

        if (!_isCreatingNewEffect)
        {
            UpdateEffectTypeSelection();
            return;
        }

        var option = _effectTypeOptions.FirstOrDefault(o => o.DisplayName == evt.newValue);
        if (option == null || option.Type == null)
        {
            UpdateEffectTypeSelection();
            return;
        }

        if (_editingEffect.GetType() == option.Type)
            return;

        ReplaceEditingEffectInstance(option.Type);
    }

    private void ReplaceEditingEffectInstance(Type type)
    {
        if (type == null || !typeof(BattleEffectSO).IsAssignableFrom(type))
            return;

        if (_editingEffect != null)
        {
            DestroyImmediate(_editingEffect);
            _editingEffect = null;
        }

        _editingEffect = (BattleEffectSO)CreateInstance(type);
        if (_editingEffect != null)
        {
            _editingEffect.hideFlags = HideFlags.HideAndDontSave;
        }

        PopulateFields();
        UpdateContentState();
        MarkDirty();
    }

    private void SetupIconDragAndDrop()
    {
        if (_iconDropArea == null)
            return;

        _iconDropArea.RegisterCallback<DragUpdatedEvent>(evt =>
        {
            if (HasSpriteReference())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        });

        _iconDropArea.RegisterCallback<DragPerformEvent>(evt =>
        {
            if (!HasSpriteReference())
                return;

            DragAndDrop.AcceptDrag();
            var sprite = DragAndDrop.objectReferences.OfType<Sprite>().FirstOrDefault();
            if (sprite != null && _editingEffect != null)
            {
                _editingEffect.Icon = sprite;
                UpdateIconPreview();
                MarkDirty();
            }
        });
    }

    private void RefreshEffects()
    {
        if (!AssetDatabase.IsValidFolder(EffectsFolderPath))
        {
            _allEffects.Clear();
            ApplyFilter();
            return;
        }

        _allEffects.Clear();
        var guids = AssetDatabase.FindAssets("t:BattleEffectSO", new[] { EffectsFolderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var effect = AssetDatabase.LoadAssetAtPath<BattleEffectSO>(path);
            if (effect != null && !_allEffects.Contains(effect))
            {
                _allEffects.Add(effect);
            }
        }

        _allEffects.Sort((a, b) => string.Compare(GetEffectFileName(a), GetEffectFileName(b), StringComparison.OrdinalIgnoreCase));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filteredEffects.Clear();
        foreach (var effect in _allEffects)
        {
            var fileName = GetEffectFileName(effect);
            if (string.IsNullOrEmpty(_searchFilter) || fileName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _filteredEffects.Add(effect);
            }
        }

        if (_effectsList != null)
        {
            _effectsList.itemsSource = _filteredEffects;
            _effectsList.Rebuild();

            if (_selectedEffect != null)
            {
                var index = _filteredEffects.IndexOf(_selectedEffect);
                if (index >= 0)
                {
                    _effectsList.SetSelection(index);
                }
                else
                {
                    _effectsList.ClearSelection();
                }
            }
        }
    }

    private void SelectEffect(BattleEffectSO effect)
    {
        _selectedEffect = effect;
        _isCreatingNewEffect = false;

        if (_editingEffect != null)
        {
            DestroyImmediate(_editingEffect);
            _editingEffect = null;
        }

        if (_selectedEffect != null)
        {
            _editingEffect = Instantiate(_selectedEffect);
            _editingEffect.hideFlags = HideFlags.HideAndDontSave;
            PopulateFields();
        }
        else
        {
            ClearFields();
        }

        _hasUnsavedChanges = false;
        UpdateContentState();
    }

    private void PopulateFields()
    {
        if (_editingEffect == null)
            return;

        _effectNameField?.SetValueWithoutNotify(_editingEffect.Name ?? string.Empty);
        _descriptionField?.SetValueWithoutNotify(_editingEffect.Description ?? string.Empty);
        _maxTickField?.SetValueWithoutNotify(_editingEffect.MaxTick);
        UpdateFileNameLabel();

        if (_triggerField != null)
        {
            _triggerField.Init(_editingEffect.Trigger);
            _triggerField.SetValueWithoutNotify(_editingEffect.Trigger);
        }

        if (_tickTriggerField != null)
        {
            _tickTriggerField.Init(_editingEffect.TickTrigger);
            _tickTriggerField.SetValueWithoutNotify(_editingEffect.TickTrigger);
        }

        UpdateEffectTypeSelection();
        RebuildTypeSpecificFields();
        UpdateIconPreview();
        UpdateSaveButtonState();
    }

    private void ClearFields()
    {
        _effectNameField?.SetValueWithoutNotify(string.Empty);
        _descriptionField?.SetValueWithoutNotify(string.Empty);
        _maxTickField?.SetValueWithoutNotify(0);
        _triggerField?.SetValueWithoutNotify(default(BattleEffectTrigger));
        _tickTriggerField?.SetValueWithoutNotify(default(BattleEffectTrigger));
        if (_fileNameLabel != null)
        {
            _fileNameLabel.text = "-";
        }

        _typeSpecificContainer?.Clear();
        UpdateEffectTypeSelection();
        UpdateIconPreview();
        UpdateSaveButtonState();
    }

    private void RebuildTypeSpecificFields()
    {
        if (_typeSpecificContainer == null)
            return;

        _typeSpecificContainer.Clear();

        if (_editingEffect == null)
            return;

        switch (_editingEffect)
        {
            case BattleEffectDamageSO damageEffect:
                RenderDamageFields(damageEffect);
                break;
            case BattleEffectStatsModifierSO statsEffect:
                RenderStatsModifierFields(statsEffect);
                break;
            default:
                var warningLabel = new Label("Тип эффекта не поддерживается редактором");
                warningLabel.AddToClassList("effect-editor__type-warning");
                _typeSpecificContainer.Add(warningLabel);
                break;
        }
    }

    private void RenderDamageFields(BattleEffectDamageSO effect)
    {
        if (_typeSpecificContainer == null)
            return;

        var damageField = new IntegerField("Урон");
        damageField.AddToClassList("effect-editor__type-field");
        damageField.SetValueWithoutNotify(Mathf.Max(0, effect.Damage));
        damageField.RegisterValueChangedCallback(evt =>
        {
            if (_editingEffect is not BattleEffectDamageSO editing)
                return;

            var newValue = Mathf.Max(0, evt.newValue);
            editing.Damage = newValue;
            damageField.SetValueWithoutNotify(newValue);
            MarkDirty();
        });

        _typeSpecificContainer.Add(damageField);
    }

    private void RenderStatsModifierFields(BattleEffectStatsModifierSO effect)
    {
        if (_typeSpecificContainer == null)
            return;

        var listContainer = new VisualElement();
        listContainer.AddToClassList("effect-editor__type-list");
        _typeSpecificContainer.Add(listContainer);

        BuildStatModifierRows(effect, listContainer);

        var addButton = new Button(() =>
        {
            if (_editingEffect is not BattleEffectStatsModifierSO statsEffect)
                return;

            var modifiers = statsEffect.StatsModifier?.ToList() ?? new List<BattleSquadStatModifier>();
            modifiers.Add(new BattleSquadStatModifier());
            statsEffect.StatsModifier = modifiers.ToArray();
            BuildStatModifierRows(statsEffect, listContainer);
            MarkDirty();
        })
        {
            text = "Добавить модификатор"
        };
        addButton.AddToClassList("effect-editor__type-add");
        _typeSpecificContainer.Add(addButton);
    }

    private void BuildStatModifierRows(BattleEffectStatsModifierSO effect, VisualElement container)
    {
        if (container == null)
            return;

        container.Clear();

        var modifiers = effect.StatsModifier ?? Array.Empty<BattleSquadStatModifier>();
        for (var i = 0; i < modifiers.Length; i++)
        {
            var rowIndex = i;
            var row = new VisualElement();
            row.AddToClassList("effect-editor__type-row");

            var statField = new EnumField("Стат");
            statField.AddToClassList("effect-editor__type-field");
            statField.Init(modifiers[rowIndex].Stat);
            statField.SetValueWithoutNotify(modifiers[rowIndex].Stat);
            statField.RegisterValueChangedCallback(evt =>
            {
                if (_editingEffect is not BattleEffectStatsModifierSO statsEffect)
                    return;

                var current = statsEffect.StatsModifier;
                if (current == null || rowIndex < 0 || rowIndex >= current.Length)
                    return;

                current[rowIndex].Stat = (BattleSquadStat)evt.newValue;
                statsEffect.StatsModifier = current;
                MarkDirty();
            });
            row.Add(statField);

            var valueField = new FloatField("Значение");
            valueField.AddToClassList("effect-editor__type-field");
            valueField.SetValueWithoutNotify(modifiers[rowIndex].Value);
            valueField.RegisterValueChangedCallback(evt =>
            {
                if (_editingEffect is not BattleEffectStatsModifierSO statsEffect)
                    return;

                var current = statsEffect.StatsModifier;
                if (current == null || rowIndex < 0 || rowIndex >= current.Length)
                    return;

                current[rowIndex].Value = evt.newValue;
                statsEffect.StatsModifier = current;
                MarkDirty();
            });
            row.Add(valueField);

            var removeButton = new Button(() =>
            {
                if (_editingEffect is not BattleEffectStatsModifierSO statsEffect)
                    return;

                var list = statsEffect.StatsModifier?.ToList() ?? new List<BattleSquadStatModifier>();
                if (rowIndex < 0 || rowIndex >= list.Count)
                    return;

                list.RemoveAt(rowIndex);
                statsEffect.StatsModifier = list.ToArray();
                BuildStatModifierRows(statsEffect, container);
                MarkDirty();
            })
            {
                text = "Удалить"
            };
            removeButton.AddToClassList("effect-editor__type-remove");
            row.Add(removeButton);

            container.Add(row);
        }
    }

    private void UpdateFileNameLabel()
    {
        if (_fileNameLabel == null)
            return;

        var previewName = BuildFileName(_effectNameField?.value);
        _fileNameLabel.text = string.IsNullOrEmpty(previewName) ? "-" : previewName;
    }

    private void UpdateIconPreview()
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = _editingEffect != null ? _editingEffect.Icon : null;
        }

        if (_iconHintLabel != null)
        {
            _iconHintLabel.style.display = _editingEffect != null && _editingEffect.Icon != null
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }

    private void UpdateContentState()
    {
        if (_contentContainer != null)
        {
            _contentContainer.SetEnabled(_editingEffect != null);
        }

        UpdateSaveButtonState();
    }

    private void UpdateSaveButtonState()
    {
        _saveButton?.SetEnabled(_editingEffect != null && _hasUnsavedChanges);
    }

    private void MarkDirty()
    {
        _hasUnsavedChanges = true;
        UpdateSaveButtonState();
    }

    private void ConfigureEnumField<T>(EnumField field, Action<T> setter) where T : Enum
    {
        if (field == null)
            return;

        field.Init(default(T));
        field.RegisterValueChangedCallback(evt =>
        {
            if (_editingEffect == null)
                return;

            setter((T)evt.newValue);
            MarkDirty();
        });
    }

    private void ConfigureIntegerField(IntegerField field, Action<int> setter)
    {
        if (field == null)
            return;

        field.RegisterValueChangedCallback(evt =>
        {
            if (_editingEffect == null)
                return;

            var value = evt.newValue;
            setter(value);
            field.SetValueWithoutNotify(value);
            MarkDirty();
        });
    }

    private void SaveCurrentEffect()
    {
        if (_editingEffect == null)
            return;

        if (_isCreatingNewEffect)
        {
            SaveNewEffectAsset();
            return;
        }

        if (_selectedEffect == null)
            return;

        if (_selectedEffect.GetType() != _editingEffect.GetType())
        {
            EditorUtility.DisplayDialog("Тип эффекта", "Нельзя изменить тип эффекта после сохранения.", "Ок");
            UpdateEffectTypeSelection();
            return;
        }

        Undo.RecordObject(_selectedEffect, "Save Effect");
        EditorUtility.CopySerializedManagedFieldsOnly(_editingEffect, _selectedEffect);
        EditorUtility.SetDirty(_selectedEffect);

        var desiredName = BuildFileName(_editingEffect.Name);
        var path = AssetDatabase.GetAssetPath(_selectedEffect);
        if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(desiredName))
        {
            var targetName = GetUniqueAssetName(desiredName, _selectedEffect);
            var currentName = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(currentName, targetName, StringComparison.Ordinal))
            {
                AssetDatabase.RenameAsset(path, targetName);
            }
        }

        AssetDatabase.SaveAssets();
        RefreshEffects();
        _hasUnsavedChanges = false;
        UpdateSaveButtonState();
    }

    private void SaveNewEffectAsset()
    {
        EnsureEffectsFolder();

        var desiredName = BuildFileName(_editingEffect.Name);
        if (string.IsNullOrEmpty(desiredName))
        {
            desiredName = "Effect";
        }

        var targetName = GetUniqueAssetName(desiredName);
        var assetPath = Path.Combine(EffectsFolderPath, targetName + ".asset");

        var newEffect = (BattleEffectSO)CreateInstance(_editingEffect.GetType());
        EditorUtility.CopySerializedManagedFieldsOnly(_editingEffect, newEffect);
        newEffect.name = targetName;

        AssetDatabase.CreateAsset(newEffect, assetPath);
        AssetDatabase.SaveAssets();

        _isCreatingNewEffect = false;
        RefreshEffects();
        SelectEffect(newEffect);

        if (_effectsList != null)
        {
            var index = _filteredEffects.IndexOf(newEffect);
            if (index >= 0)
            {
                _effectsList.SetSelection(index);
            }
        }
    }

    private void CreateEffectAsset()
    {
        if (_editingEffect != null)
        {
            DestroyImmediate(_editingEffect);
            _editingEffect = null;
        }

        _selectedEffect = null;
        _isCreatingNewEffect = true;

        var defaultType = _effectTypeOptions.FirstOrDefault()?.Type ?? typeof(BattleEffectSO);
        _editingEffect = (BattleEffectSO)CreateInstance(defaultType);
        _editingEffect.hideFlags = HideFlags.HideAndDontSave;

        PopulateFields();
        UpdateContentState();
        MarkDirty();

        _effectsList?.ClearSelection();
    }

    private void EnsureEffectsFolder()
    {
        if (!Directory.Exists(EffectsFolderPath))
        {
            Directory.CreateDirectory(EffectsFolderPath);
        }
    }

    private string GetUniqueAssetName(string baseName, BattleEffectSO ignoreEffect = null)
    {
        var uniqueName = baseName;
        var counter = 1;
        while (true)
        {
            var path = Path.Combine(EffectsFolderPath, uniqueName + ".asset");
            var existing = AssetDatabase.LoadAssetAtPath<BattleEffectSO>(path);
            if (existing == null || existing == ignoreEffect)
            {
                return uniqueName;
            }

            uniqueName = baseName + counter;
            counter++;
        }
    }

    private static string GetEffectFileName(BattleEffectSO effect)
    {
        if (effect == null)
            return string.Empty;

        var path = AssetDatabase.GetAssetPath(effect);
        return string.IsNullOrEmpty(path)
            ? effect.name
            : Path.GetFileNameWithoutExtension(path);
    }

    private static string BuildFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var parts = value.Split(new[] { ' ', '\t', '\n', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0)
                continue;

            var lower = part.ToLowerInvariant();
            var c = char.ToUpperInvariant(lower[0]);
            builder.Append(c);
            if (lower.Length > 1)
            {
                builder.Append(lower.Substring(1));
            }
        }

        return builder.Length == 0 ? string.Empty : builder.ToString();
    }

    private bool HasSpriteReference()
    {
        return DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.OfType<Sprite>().Any();
    }

    private sealed class EffectTypeOption
    {
        public EffectTypeOption(Type type)
        {
            Type = type;
            DisplayName = type?.Name ?? string.Empty;
        }

        public string DisplayName { get; }
        public Type Type { get; }
    }
}
