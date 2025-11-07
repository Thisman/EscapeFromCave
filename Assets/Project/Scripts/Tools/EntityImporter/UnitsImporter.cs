using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;

public sealed class UnitsImporter
{
    private readonly UnitsImportSettingsSO _settings;

    public UnitsImporter(UnitsImportSettingsSO settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Import(bool revealInFinder = true)
    {
        var tableText = ImporterTableLoader.Download(_settings.TableUrl, "UnitsImporter", _settings.Delimiter);
        if (string.IsNullOrWhiteSpace(tableText)) { Debug.LogWarning("[UnitsImporter] Table text is empty"); return; }

        var rootPath = AssetDatabase.GetAssetPath(_settings.RootFolder);
        if (string.IsNullOrEmpty(rootPath) || !AssetDatabase.IsValidFolder(rootPath))
        {
            Debug.LogWarning("[UnitsImporter] RootFolder is not set or invalid");
            return;
        }

        // 1) Разбор таблицы и материализация строк (важно до предсоздания подпапок)
        var rows = ParseTable(tableText, _settings.Delimiter, _settings.HasHeader).ToList();

        // 2) Создаём подпапки по Kind при необходимости
        EnsureKindSubfolders(rootPath, rows);

        // 3) Создание/обновление ассетов
        int ok = 0, bad = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var row in rows)
            {
                if (TryCreateUnitAsset(row, _settings, rootPath, out _))
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

        Debug.Log($"[UnitsImporter] Done. OK: {ok}, Warnings: {bad}");
        if (revealInFinder)
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(rootPath));
        }
    }

    // =========================
    // Предсоздание подпапок по Kind
    // =========================
    private static void EnsureKindSubfolders(string rootPath, IEnumerable<Dictionary<string, string>> rows)
    {
        var kinds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            if (TryEnumAny(r, out UnitKind kind, "UnitKind", "Kind"))
                kinds.Add(kind.ToString());
        }

        foreach (var k in kinds)
        {
            var path = $"{rootPath}/{k}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(rootPath, k);
        }
    }

    private static string GetKindFolder(string rootPath, UnitKind kind)
        => $"{rootPath}/{kind}";

    // =========================
    // Парсер таблицы (CSV/TSV, RFC-4180)
    // =========================
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

    // Разбор одной строки CSV/TSV с кавычками и экранированием ""
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
                    // Экранированная кавычка ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // пропустить вторую кавычку
                    }
                    else
                    {
                        inQuotes = false; // закрыли кавычки
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
                    inQuotes = true; // открыли кавычки
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

    // =========================
    // Создание ассета из строки
    // =========================
    private static bool TryCreateUnitAsset(Dictionary<string, string> r, UnitsImportSettingsSO s, string rootPath, out string createdPath)
    {
        createdPath = null;

        // 1) Название (обязательное)
        string unitName = GetAnyValue(r, "UnitName", "Name");
        if (string.IsNullOrWhiteSpace(unitName))
        {
            Warn("UnitName empty", r);
            return false;
        }

        // 2) Энамы (поддержка синонимов колонок)
        if (!TryEnumAny(r, out UnitKind kind, "UnitKind", "Kind"))
        {
            Warn("Bad Kind", r);
            return false;
        }

        if (!TryEnumAny(r, out AttackKind attackKind, "AttackKind", "AttackType"))
        {
            Warn("Bad AttackKind", r);
            return false;
        }

        if (!TryEnumAny(r, out DamageType damageType, "DamageType", "Damage"))
        {
            Warn("Bad DamageType", r);
            return false;
        }

        // 3) Числа/проценты (без округлений)
        bool ok = true;

        float baseHealth = ReadF(r, FirstExistingKey(r, "BaseHealth"), 100, ref ok);
        float pDef = ReadPct(r, FirstExistingKey(r, "BasePhysicalDefense", "PhysicalDefense"), 0, s.AutoNormalizePercents, ref ok);
        float mDef = ReadPct(r, FirstExistingKey(r, "BaseMagicDefense", "MagicDefense"), 0, s.AutoNormalizePercents, ref ok);
        float aDef = ReadPct(r, FirstExistingKey(r, "BaseAbsoluteDefense", "AbsoluteDefense"), 0, s.AutoNormalizePercents, ref ok);
        float minDmg = ReadF(r, FirstExistingKey(r, "MinDamage", "DamageMin"), 10, ref ok);
        float maxDmg = ReadF(r, FirstExistingKey(r, "MaxDamage", "DamageMax"), 20, ref ok);
        float speed = ReadF(r, FirstExistingKey(r, "Speed", "InitSpeed"), 2, ref ok);
        float critCh = ReadPct(r, FirstExistingKey(r, "BaseCritChance", "CritChance"), 0, s.AutoNormalizePercents, ref ok);
        float critMul = ReadF(r, FirstExistingKey(r, "BaseCritMultiplier", "CritMultiplier"), 1.1f, ref ok);
        float missCh = ReadPct(r, FirstExistingKey(r, "BaseMissChance", "MissChance"), 0, s.AutoNormalizePercents, ref ok);

        if (!ok) { Warn("Number parse error(s)", r); }

        if (minDmg > maxDmg)
        {
            Warn("MinDamage > MaxDamage; swapped", r);
            (minDmg, maxDmg) = (maxDmg, minDmg);
        }
        if (speed < 1)
        {
            Warn("Speed < 1; clamped to 1", r);
            speed = 1;
        }

        // 4) Иконка
        int iconIndex = ReadInt(r, FirstExistingKey(r, "Icon", "IconIndex"), -1, ref ok);
        Sprite icon = ResolveIcon(kind, iconIndex, s);

        // 5) Создание ассета и запись (папка уже создана заранее)
        string kindFolder = GetKindFolder(rootPath, kind);

        string fileName = $"{San(unitName.Trim())}_{kind}_{attackKind}.asset";
        string targetPath = $"{kindFolder}/{fileName}";

        var asset = AssetDatabase.LoadAssetAtPath<UnitDefinitionSO>(targetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<UnitDefinitionSO>();
            AssetDatabase.CreateAsset(asset, targetPath);
        }

        asset.Icon = icon;
        asset.UnitName = unitName.Trim();
        asset.Kind = kind;
        asset.AttackKind = attackKind;
        asset.DamageType = damageType;

        asset.BaseHealth = baseHealth;
        asset.BasePhysicalDefense = Mathf.Clamp01(pDef);
        asset.BaseMagicDefense = Mathf.Clamp01(mDef);
        asset.BaseAbsoluteDefense = Mathf.Clamp01(aDef);

        asset.MinDamage = Mathf.Max(0, minDmg);
        asset.MaxDamage = Mathf.Max(asset.MinDamage, maxDmg);
        asset.Speed = Mathf.Max(1, speed);

        asset.BaseCritChance = Mathf.Clamp01(critCh);
        asset.BaseCritMultiplier = Mathf.Max(1, critMul);
        asset.BaseMissChance = Mathf.Clamp01(missCh);

        asset.Abilities = System.Array.Empty<BattleAbilityDefinitionSO>();
        EditorUtility.SetDirty(asset);
        createdPath = targetPath;
        return true;
    }

    // =========================
    // Helpers: чтение/поиск колонок
    // =========================

    // Имя первого существующего ключа, иначе вернёт первый из списка
    static string FirstExistingKey(Dictionary<string, string> r, params string[] keys)
    {
        foreach (var k in keys)
            if (r.ContainsKey(k)) return k;
        return keys.Length > 0 ? keys[0] : null;
    }

    // Значение по первому найденному ключу
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
        => Debug.LogWarning($"[UnitsImporter] {msg}; row: {string.Join(" | ", r.Select(kv => $"{kv.Key}={kv.Value}"))}");

    // Чтение чисел/процентов — БЕЗ округлений
    static float ReadF(Dictionary<string, string> r, string key, float def, ref bool ok)
    {
        if (string.IsNullOrEmpty(key) || !r.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
            return def;

        s = s.Trim().Replace(',', '.'); // поддержка "0,2"
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;

        ok = false;
        return def;
    }

    static float ReadPct(Dictionary<string, string> r, string key, float def, bool autoNormalize, ref bool ok)
    {
        var v = ReadF(r, key, def, ref ok);
        if (autoNormalize && v > 1f) v /= 100f; // масштабирование, НЕ округление
        return v;
    }

    static int ReadInt(Dictionary<string, string> r, string key, int def, ref bool ok)
    {
        if (string.IsNullOrEmpty(key) || !r.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
            return def;

        s = s.Trim();
        if (int.TryParse(s, out var v)) return v;

        ok = false;
        return def;
    }

    // =========================
    // Иконки (только Sprite[] из Settings)
    // =========================
    static Sprite ResolveIcon(UnitKind kind, int index, UnitsImportSettingsSO s)
    {
        if (index < 0) return null;

        Sprite FromList(Sprite[] arr) => (arr != null && index >= 0 && index < arr.Length) ? arr[index] : null;

        switch (kind)
        {
            case UnitKind.Ally: return FromList(s.AllySprites);
            case UnitKind.Hero: return FromList(s.HeroSprites);
            case UnitKind.Enemy: return FromList(s.EnemySprites);
            case UnitKind.Neutral: return FromList(s.NeutralSprites);
            default: return null;
        }
    }

    // =========================
    // Санитизация имени файла
    // =========================
    static string San(string s)
    {
        if (string.IsNullOrEmpty(s)) return "Unit";
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
