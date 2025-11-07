using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                    var result = await operation.ExecuteAsync();

                    if (result.Logs != null && result.Logs.Count > 0)
                    {
                        _importLog.AddRange(result.Logs);
                    }

                    if (result.AssetsModified)
                    {
                        anyAssetsModified = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EntitiesImporter: Failed to process table '{operation.Name}'. {ex.Message}");
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
                () => ProcessBattleEffectsAsync(_settings.BattleEffectsTableUrl, parser, creator));
        }
    }

    private async Task<TableImportResult> ProcessBattleEffectsAsync(
        string tableUrl,
        BattleEffectsTableParser parser,
        BattleEffectsCreator creator)
    {
        var logs = new List<string>();

        if (string.IsNullOrWhiteSpace(tableUrl))
        {
            logs.Add("Battle Effects table URL is empty.");
            return new TableImportResult(0, false, logs);
        }

        var sheetResults = await parser.LoadAllSheetsAsync(
            _tableLoader,
            tableUrl,
            _settings.Delimiter);

        if (sheetResults == null || sheetResults.Count == 0)
        {
            logs.Add("No battle effect sheets were returned from the parser.");
            return new TableImportResult(0, false, logs);
        }

        var processedCount = 0;

        foreach (var sheetResult in sheetResults)
        {
            logs.Add($"Loaded {sheetResult.RawRowCount} rows from '{sheetResult.SheetName}' sheet.");

            foreach (var entry in sheetResult.Entries)
            {
                creator.Create(entry);
            }

            processedCount += sheetResult.Entries.Count;
        }

        logs.Add($"Processed {processedCount} rows for Battle Effects table.");

        var assetsModified = processedCount > 0;

        return new TableImportResult(processedCount, assetsModified, logs);
    }

    private readonly struct TableImportOperation
    {
        public TableImportOperation(string name, Func<Task<TableImportResult>> executeAsync)
        {
            Name = name;
            ExecuteAsync = executeAsync;
        }

        public string Name { get; }
        public Func<Task<TableImportResult>> ExecuteAsync { get; }
    }

    private readonly struct TableImportResult
    {
        public TableImportResult(int processedCount, bool assetsModified, IReadOnlyList<string> logs)
        {
            ProcessedCount = processedCount;
            AssetsModified = assetsModified;
            Logs = logs;
        }

        public int ProcessedCount { get; }
        public bool AssetsModified { get; }
        public IReadOnlyList<string> Logs { get; }
    }
}
