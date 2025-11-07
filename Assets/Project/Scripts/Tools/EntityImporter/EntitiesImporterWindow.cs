using System;
using UnityEditor;
using UnityEngine;

public sealed class EntitiesImporterWindow : EditorWindow
{
    [SerializeField] private StatModifiersImportSettingsSO _statModifiersSettings;
    [SerializeField] private DamageEffectsImportSettingsSO _damageEffectsSettings;
    [SerializeField] private AbilitiesImportSettingsSO _abilitiesSettings;
    [SerializeField] private UnitsImportSettingsSO _unitsSettings;
    [SerializeField] private ObjectsImportSettingsSO _objectsSettings;

    private bool _isImporting;
    private string _currentStep = "Ожидание";

    [MenuItem("Tools/Entities Importer")]
    private static void Open() => GetWindow<EntitiesImporterWindow>("Entities Importer");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Настройки", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(_isImporting);
        _statModifiersSettings = (StatModifiersImportSettingsSO)EditorGUILayout.ObjectField("Stat Modifiers", _statModifiersSettings, typeof(StatModifiersImportSettingsSO), false);
        _damageEffectsSettings = (DamageEffectsImportSettingsSO)EditorGUILayout.ObjectField("Damage Effects", _damageEffectsSettings, typeof(DamageEffectsImportSettingsSO), false);
        _abilitiesSettings = (AbilitiesImportSettingsSO)EditorGUILayout.ObjectField("Abilities", _abilitiesSettings, typeof(AbilitiesImportSettingsSO), false);
        _unitsSettings = (UnitsImportSettingsSO)EditorGUILayout.ObjectField("Units", _unitsSettings, typeof(UnitsImportSettingsSO), false);
        _objectsSettings = (ObjectsImportSettingsSO)EditorGUILayout.ObjectField("Objects", _objectsSettings, typeof(ObjectsImportSettingsSO), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Импортировать все", GUILayout.Height(32)))
        {
            StartImport();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Текущий шаг", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(_currentStep ?? string.Empty);
    }

    private void StartImport()
    {
        if (_isImporting)
            return;

        if (!ValidateSettings())
            return;

        _isImporting = true;
        _currentStep = "Подготовка...";
        RemoveNotification();
        Repaint();

        EditorApplication.delayCall += RunImportSequence;
    }

    private bool ValidateSettings()
    {
        if (_statModifiersSettings == null)
        {
            ShowNotification(new GUIContent("Укажите StatModifiers settings"));
            return false;
        }

        if (_damageEffectsSettings == null)
        {
            ShowNotification(new GUIContent("Укажите DamageEffects settings"));
            return false;
        }

        if (_abilitiesSettings == null)
        {
            ShowNotification(new GUIContent("Укажите Abilities settings"));
            return false;
        }

        if (_unitsSettings == null)
        {
            ShowNotification(new GUIContent("Укажите Units settings"));
            return false;
        }

        if (_objectsSettings == null)
        {
            ShowNotification(new GUIContent("Укажите Objects settings"));
            return false;
        }

        return true;
    }

    private void RunImportSequence()
    {
        EditorApplication.delayCall -= RunImportSequence;

        try
        {
            RunImporter("Stat modifiers", () => new StatModifiersImporter(_statModifiersSettings).Import(false));
            RunImporter("Damage effects", () => new DamageEffectsImporter(_damageEffectsSettings).Import(false));
            RunImporter("Abilities", () => new AbilitiesImporter(_abilitiesSettings).Import(false));
            RunImporter("Units", () => new UnitsImporter(_unitsSettings).Import(false));
            RunImporter("Objects", () => new ObjectsImporter(_objectsSettings).Import(false));

            ShowNotification(new GUIContent("Импорт завершён"));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EntitiesImporter] Ошибка: {ex}");
            ShowNotification(new GUIContent("Ошибка импорта. Подробности в Console."));
        }
        finally
        {
            _isImporting = false;
            _currentStep = "Готово";
            RemoveNotification();
            Repaint();
        }
    }

    private void RunImporter(string step, Action action)
    {
        _currentStep = step;
        Debug.Log($"[EntitiesImporter] {step}");
        Repaint();

        action?.Invoke();
    }
}
