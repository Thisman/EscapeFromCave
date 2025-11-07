using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EntitiesImporter : EditorWindow
{
    private EntitiesImporterSettingsSO _settings;
    private EntityTableLoader _tableLoader;
    private bool _isImporting;
    private readonly List<string> _importLog = new List<string>();

    [MenuItem("Tools/Entities Importer")]
    public static void ShowWindow()
    {
        GetWindow<EntitiesImporter>("Entities Importer");
    }

    private void OnEnable()
    {
        _tableLoader ??= new EntityTableLoader();
    }

    private void OnDisable()
    {
        _tableLoader = null;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Entities Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _settings = (EntitiesImporterSettingsSO)EditorGUILayout.ObjectField(
            "Settings",
            _settings,
            typeof(EntitiesImporterSettingsSO),
            false);

        using (new EditorGUI.DisabledScope(_isImporting))
        {
            if (GUILayout.Button("Import"))
            {
                ImportTablesAsync();
            }
        }

        if (_importLog.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import Log", EditorStyles.boldLabel);
            foreach (var message in _importLog)
            {
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
        }
    }

    private async void ImportTablesAsync()
    {
        if (_settings == null)
        {
            Debug.LogWarning("EntitiesImporter: Settings are not assigned.");
            return;
        }

        var urls = _settings.GetAllTableUrls().Where(url => !string.IsNullOrWhiteSpace(url)).ToList();
        if (urls.Count == 0)
        {
            Debug.LogWarning("EntitiesImporter: No table URLs specified in the settings.");
            return;
        }

        _isImporting = true;
        _importLog.Clear();

        try
        {
            foreach (var url in urls)
            {
                try
                {
                    var content = await _tableLoader.LoadTableAsync(url);
                    _importLog.Add($"Loaded table from {url} ({content.Length} characters).");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EntitiesImporter: Failed to load table from {url}. {ex.Message}");
                    _importLog.Add($"Failed to load table from {url}. See console for details.");
                }
            }
        }
        finally
        {
            _isImporting = false;
            Repaint();
        }
    }
}
