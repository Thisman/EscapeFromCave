using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

public sealed class ObjectsImporterWindow : EditorWindow
{
    private ObjectsImportSettingsSO _settings;

    [MenuItem("Tools/Objects Importer")]
    private static void Open() => GetWindow<ObjectsImporterWindow>("Objects Importer");

    private void OnGUI()
    {
        _settings = (ObjectsImportSettingsSO)EditorGUILayout.ObjectField("Settings", _settings, typeof(ObjectsImportSettingsSO), false);
        if (_settings == null) { EditorGUILayout.HelpBox("Укажите Settings (ObjectsImportSettingsSO)", MessageType.Info); return; }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Источник", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Delimiter:", _settings.Delimiter == '\t' ? "\\t (TSV)" : _settings.Delimiter.ToString());
        EditorGUILayout.LabelField("HasHeader:", _settings.HasHeader ? "true" : "false");

        EditorGUILayout.Space();
        if (GUILayout.Button("Import (update/create)", GUILayout.Height(32)))
        {
            ImportAll(_settings);
        }
    }

    private void ImportAll(ObjectsImportSettingsSO s)
    {
        var tableText = ImporterTableLoader.Download(s.TableUrl, "ObjectsImporter");
        if (string.IsNullOrWhiteSpace(tableText)) { Debug.LogWarning("[ObjectsImporter] Table text is empty"); return; }

        var rootPath = AssetDatabase.GetAssetPath(s.RootFolder);
        if (string.IsNullOrEmpty(rootPath) || !AssetDatabase.IsValidFolder(rootPath))
        {
            Debug.LogWarning("[ObjectsImporter] RootFolder is not set or invalid");
            return;
        }

        // 1) Разбор таблицы и материализация строк
        var rows = ParseTable(tableText, s.Delimiter, s.HasHeader).ToList();

        // 2) Создание/обновление ассетов
        int ok = 0, bad = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var row in rows)
            {
                if (TryCreateObjectAsset(row, s, rootPath, out _))
                    ok++;
                else
                    bad++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[ObjectsImporter] Done. OK: {ok}, Warnings: {bad}");
        EditorUtility.RevealInFinder(Path.GetFullPath(rootPath));
    }

    // ===== CSV/TSV парсер (RFC-4180) =====
    private static IEnumerable<Dictionary<string, string>> ParseTable(string text, char delimiter, bool header)
    {
        var reader = new StringReader(text);
        string line;
        string[] headerCols = null;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvLine(line, delimiter);

            if (header && headerCols == null)
            {
                headerCols = cols.Select(c => c.Trim()).ToArray();
                continue;
            }

            var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            if (headerCols != null)
            {
                int len = Mathf.Min(headerCols.Length, cols.Count);
                for (int i = 0; i < len; i++)
                    map[headerCols[i]] = cols[i].Trim();
            }
            else
            {
                for (int i = 0; i < cols.Count; i++)
                    map[$"Col{i}"] = cols[i].Trim();
            }

            yield return map;
        }
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        if (line == null) { result.Add(string.Empty); return result; }

        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    result.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        result.Add(current.ToString());
        return result;
    }

    // ===== Создание ассета из строки =====
    private static bool TryCreateObjectAsset(Dictionary<string, string> r, ObjectsImportSettingsSO s, string rootPath, out string createdPath)
    {
        createdPath = null;

        // Обязательное имя
        string objectName = GetAnyValue(r, "ObjectName", "Name");
        if (string.IsNullOrWhiteSpace(objectName))
        {
            Warn("ObjectName empty", r);
            return false;
        }

        // Icon (индекс)
        bool ok = true;
        int iconIndex = ReadInt(r, FirstExistingKey(r, "Icon", "IconIndex"), -1, ref ok);
        if (!ok) { Warn("Icon parse error", r); }

        Sprite icon = ResolveIcon(iconIndex, s);

        // Создание ассета
        string sanitizedName = San(objectName.Trim());
        string targetPath = $"{rootPath}/{sanitizedName}.asset";

        var asset = AssetDatabase.LoadAssetAtPath<ObjectDefinitionSO>(targetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ObjectDefinitionSO>();
            AssetDatabase.CreateAsset(asset, targetPath);
        }

        asset.ObjectName = objectName.Trim();
        asset.Icon = icon;
        EditorUtility.SetDirty(asset);

        createdPath = targetPath;
        return true;
    }

    // ===== Helpers =====

    static string FirstExistingKey(Dictionary<string, string> r, params string[] keys)
    {
        foreach (var k in keys)
            if (r.ContainsKey(k)) return k;
        return keys.Length > 0 ? keys[0] : null;
    }

    static string GetAnyValue(Dictionary<string, string> r, params string[] keys)
    {
        foreach (var k in keys)
            if (r.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;
        return null;
    }

    static void Warn(string msg, Dictionary<string, string> r)
        => Debug.LogWarning($"[ObjectsImporter] {msg}; row: {string.Join(" | ", r.Select(kv => $"{kv.Key}={kv.Value}"))}");

    static int ReadInt(Dictionary<string, string> r, string key, int def, ref bool ok)
    {
        if (string.IsNullOrEmpty(key) || !r.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
            return def;

        s = s.Trim();
        if (int.TryParse(s, out var v)) return v;

        ok = false;
        return def;
    }

    static Sprite ResolveIcon(int index, ObjectsImportSettingsSO s)
    {
        if (index < 0) return null;
        var arr = s.Sprites;
        return (arr != null && index >= 0 && index < arr.Length) ? arr[index] : null;
    }

    static string San(string s)
    {
        if (string.IsNullOrEmpty(s)) return "Object";
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
