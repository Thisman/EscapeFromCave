using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System;
using System.Reflection;

public sealed class StatModifiersImporterWindow : EditorWindow
{
    private StatModifiersImportSettingsSO _settings;

    [MenuItem("Tools/Stat Modifiers Importer")]
    private static void Open() => GetWindow<StatModifiersImporterWindow>("Stat Modifiers Importer");

    private void OnGUI()
    {
        _settings = (StatModifiersImportSettingsSO)EditorGUILayout.ObjectField("Settings", _settings, typeof(StatModifiersImportSettingsSO), false);
        if (_settings == null) { EditorGUILayout.HelpBox("Укажите Settings (StatModifiersImportSettingsSO)", MessageType.Info); return; }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Источник", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Delimiter:", _settings.Delimiter == '\t' ? "\\t (TSV)" : _settings.Delimiter.ToString());
        EditorGUILayout.LabelField("HasHeader:", _settings.HasHeader ? "true" : "false");

        EditorGUILayout.Space();
        if (GUILayout.Button("Import (clear & rebuild)", GUILayout.Height(32)))
        {
            ImportAll(_settings);
        }
    }

    private void ImportAll(StatModifiersImportSettingsSO s)
    {
        if (s.Table == null) { Debug.LogWarning("[StatModsImporter] Table is null"); return; }

        var rootPath = AssetDatabase.GetAssetPath(s.RootFolder);
        if (string.IsNullOrEmpty(rootPath) || !AssetDatabase.IsValidFolder(rootPath))
        {
            Debug.LogWarning("[StatModsImporter] RootFolder is not set or invalid");
            return;
        }

        // 1) Очистка и Refresh
        ClearRootFolder(rootPath);
        AssetDatabase.Refresh();

        // 2) Разбор таблицы
        var rows = ParseTable(s.Table.text, s.Delimiter, s.HasHeader).ToList();

        // 3) Создание ассетов
        int ok = 0, bad = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var row in rows)
            {
                if (TryCreateStatModifierEffectAsset(row, s, rootPath, out _))
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

        Debug.Log($"[StatModsImporter] Done. OK: {ok}, Warnings: {bad}");
        EditorUtility.RevealInFinder(Path.GetFullPath(rootPath));
    }

    // ===== Очистка целевой папки =====
    private static void ClearRootFolder(string rootPath)
    {
        var guids = AssetDatabase.FindAssets("", new[] { rootPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == rootPath) continue;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.DeleteAsset(path);
        }

        var folders = new List<string>(AssetDatabase.GetSubFolders(rootPath));
        for (int i = 0; i < folders.Count; i++)
            folders.AddRange(AssetDatabase.GetSubFolders(folders[i]));
        folders.Sort((a, b) => b.Length.CompareTo(a.Length));
        foreach (var f in folders.Distinct())
            if (AssetDatabase.IsValidFolder(f))
                AssetDatabase.DeleteAsset(f);
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
    private static bool TryCreateStatModifierEffectAsset(
        Dictionary<string, string> r,
        StatModifiersImportSettingsSO s,
        string rootPath,
        out string createdPath)
    {
        createdPath = null;

        // 1) Имя (обязательное)
        string displayName = GetAnyValue(r, "Name", "EffectName");
        if (string.IsNullOrWhiteSpace(displayName))
        {
            Warn("Name empty", r);
            return false;
        }

        // 2) Описание
        string description = GetAnyValue(r, "Description", "Desc") ?? "";

        // 3) Иконка по индексу
        bool ok = true;
        int iconIndex = ReadInt(r, FirstExistingKey(r, "Icon", "IconIndex"), -1, ref ok);
        if (!ok) { Warn("Icon parse error", r); ok = true; } // не критично
        Sprite icon = ResolveIcon(iconIndex, s);

        // 4) Триггер
        if (!TryEnumAny(r, out BattleEffectTrigger trigger, "Trigger"))
        {
            Warn("Bad Trigger", r);
            return false;
        }

        // 5) MaxTick (int >= 0)
        int maxTick = ReadInt(r, FirstExistingKey(r, "MaxTick", "Max Ticks"), 0, ref ok);
        if (maxTick < 0) { Warn("MaxTick < 0; clamped to 0", r); maxTick = 0; }

        // 6) Stat / Value → StatsModifier[1]
        string statRaw = GetAnyValue(r, "Stat");
        string valueRaw = GetAnyValue(r, "Value");
        if (string.IsNullOrWhiteSpace(statRaw) || string.IsNullOrWhiteSpace(valueRaw))
        {
            Warn("Stat or Value empty", r);
            // продолжим, но модификатор пустой
        }

        // Сконструировать BattleStatModifier через рефлексию,
        // чтобы не зависеть от точного определения (enum/string/int для Stat; float/int для Value)
        var modifier = CreateBattleStatModifier(statRaw, valueRaw, out bool modOk);
        if (!modOk) Warn("Failed to build BattleStatModifier (Stat/Value parse)", r);

        // 7) Создание ассета (тип StatModifierBattleEffect)
        var asset = ScriptableObject.CreateInstance<StatModifierBattleEffect>();
        // базовые поля
        asset.Name = displayName.Trim();
        asset.Description = description;
        asset.Icon = icon;
        asset.Trigger = trigger;
        asset.MaxTick = maxTick;
        // массив модификаторов
        asset.StatsModifier = new[] { modifier };

        string fileName = $"{San(asset.Name)}.asset";
        string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{rootPath}/{fileName}");
        AssetDatabase.CreateAsset(asset, targetPath);

        createdPath = targetPath;
        return true;
    }

    // ----- Построение BattleStatModifier из пары Stat/Value (рефлексия) -----
    private static BattleStatModifier CreateBattleStatModifier(string statRaw, string valueRaw, out bool ok)
    {
        ok = true;

        // 1) распарсить value как float (без округления)
        float value = 0f;
        if (!string.IsNullOrWhiteSpace(valueRaw))
        {
            var s = valueRaw.Trim().Replace(',', '.');
            if (!float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                ok = false;
                value = 0f;
            }
        }
        else ok = false;

        // 2) сконструировать и заполнить через рефлексию
        var mod = default(BattleStatModifier);
        object boxed = mod; // boxing для редактирования полей структур

        var t = boxed.GetType();

        // Найти поле/свойство Stat
        var statField = (FieldInfo)t.GetField("Stat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var statProp = (PropertyInfo)t.GetProperty("Stat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var statType = (Type)(statField != null ? statField.FieldType : statProp?.PropertyType);

        // Найти поле/свойство Value
        var valField = (FieldInfo)t.GetField("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var valProp = (PropertyInfo)t.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var valType = (Type)(valField != null ? valField.FieldType : valProp?.PropertyType);

        // Установить Stat
        if (statType != null)
        {
            object statValueObj = null;

            if (statType.IsEnum)
            {
                if (!string.IsNullOrWhiteSpace(statRaw) && Enum.TryParse(statType, statRaw.Trim(), true, out var enumVal))
                    statValueObj = enumVal;
                else
                    ok = false;
            }
            else if (statType == typeof(string))
            {
                statValueObj = statRaw?.Trim() ?? string.Empty;
            }
            else if (statType == typeof(int))
            {
                if (int.TryParse(statRaw?.Trim(), out var iv)) statValueObj = iv; else ok = false;
            }
            else
            {
                // непредусмотренный тип
                ok = false;
            }

            if (ok && statValueObj != null)
            {
                if (statField != null) statField.SetValue(boxed, statValueObj);
                else if (statProp != null && statProp.CanWrite) statProp.SetValue(boxed, statValueObj);
            }
        }
        else ok = false;

        // Установить Value
        if (valType != null)
        {
            object vObj = null;
            if (valType == typeof(float)) vObj = value;
            else if (valType == typeof(double)) vObj = (double)value;
            else if (valType == typeof(int)) vObj = (int)Mathf.RoundToInt(value); // если int — придётся привести
            else
            {
                ok = false;
            }

            if (ok && vObj != null)
            {
                if (valField != null) valField.SetValue(boxed, vObj);
                else if (valProp != null && valProp.CanWrite) valProp.SetValue(boxed, vObj);
            }
        }
        else ok = false;

        // вернуть распакованную структуру
        mod = (BattleStatModifier)boxed;
        return mod;
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

    static bool TryEnumAny<T>(Dictionary<string, string> r, out T val, params string[] keys) where T : struct
    {
        val = default;
        var s = GetAnyValue(r, keys);
        return !string.IsNullOrEmpty(s) && System.Enum.TryParse<T>(s.Trim(), true, out val);
    }

    static void Warn(string msg, Dictionary<string, string> r)
        => Debug.LogWarning($"[StatModsImporter] {msg}; row: {string.Join(" | ", r.Select(kv => $"{kv.Key}={kv.Value}"))}");

    static int ReadInt(Dictionary<string, string> r, string key, int def, ref bool ok)
    {
        if (string.IsNullOrEmpty(key) || !r.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
            return def;

        s = s.Trim();
        if (int.TryParse(s, out var v)) return v;

        ok = false;
        return def;
    }

    static Sprite ResolveIcon(int index, StatModifiersImportSettingsSO s)
    {
        if (index < 0) return null;
        var arr = s.Sprites;
        return (arr != null && index >= 0 && index < arr.Length) ? arr[index] : null;
    }

    static string San(string s)
    {
        if (string.IsNullOrEmpty(s)) return "StatModifierEffect";
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
