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

        var operations = BuildImportOperations().ToList();
        if (operations.Count == 0)
        {
            Debug.LogWarning("EntitiesImporter: No table URLs specified in the settings.");
            return;
        }

        _isImporting = true;
        _importLog.Clear();

        try
        {
            var anyAssetsModified = false;

            foreach (var operation in operations)
            {
                try
                {
                    var content = await _tableLoader.LoadTableAsync(operation.Url);
                    var characterCount = content?.Length ?? 0;
                    _importLog.Add($"Loaded {operation.Name} table from {operation.Url} ({characterCount} characters).");

                    if (operation.ProcessContent != null)
                    {
                        var result = operation.ProcessContent(content);
                        _importLog.Add($"Processed {result.ProcessedCount} rows for {operation.Name} table.");

                        if (result.AssetsModified)
                        {
                            anyAssetsModified = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EntitiesImporter: Failed to process table '{operation.Name}' from {operation.Url}. {ex.Message}");
                    _importLog.Add($"Failed to import {operation.Name} table. See console for details.");
                }
            }

#if UNITY_EDITOR
            if (anyAssetsModified)
            {
                AssetDatabase.Refresh();
            }
#endif
        }
        finally
        {
            _isImporting = false;
            Repaint();
        }
    }

    private IEnumerable<TableImportOperation> BuildImportOperations()
    {
        if (_settings == null)
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(_settings.BattleEffectsTableUrl))
        {
            var parser = new BattleEffectsTableParser();
            var creator = new BattleEffectsCreator(_settings);

            yield return new TableImportOperation(
                "Battle Effects",
                _settings.BattleEffectsTableUrl,
                content => ProcessBattleEffects(content, parser, creator));
        }
    }

    private TableImportResult ProcessBattleEffects(string content, BattleEffectsTableParser parser, BattleEffectsCreator creator)
    {
        var processedCount = 0;
        var assetsModified = false;

        if (!string.IsNullOrWhiteSpace(content))
        {
            var entries = parser
                .Parse(content, _settings.Delimiter)
                .ToList();

            foreach (var entry in entries)
            {
                creator.Create(entry);
            }

            processedCount = entries.Count;
            assetsModified = processedCount > 0;
        }

        return new TableImportResult(processedCount, assetsModified);
    }

    private readonly struct TableImportOperation
    {
        public TableImportOperation(string name, string url, Func<string, TableImportResult> processContent)
        {
            Name = name;
            Url = url;
            ProcessContent = processContent;
        }

        public string Name { get; }
        public string Url { get; }
        public Func<string, TableImportResult> ProcessContent { get; }
    }

    private readonly struct TableImportResult
    {
        public TableImportResult(int processedCount, bool assetsModified)
        {
            ProcessedCount = processedCount;
            AssetsModified = assetsModified;
        }

        public int ProcessedCount { get; }
        public bool AssetsModified { get; }
    }
}
