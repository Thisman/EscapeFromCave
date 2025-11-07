#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

internal static class DownloadedTablesProcessor
{
    private const string DamageBattleEffectSheetName = "DamageBattleEffect";

    public static void Process(IEnumerable<FileDownloader.DownloadResult> results, DownloadSettings settings)
    {
        if (results == null || settings == null)
        {
            return;
        }

        foreach (var result in results)
        {
            if (result == null || !string.IsNullOrEmpty(result.error) || result.data == null || result.data.Length == 0)
            {
                continue;
            }

            try
            {
                ProcessDamageBattleEffectSheet(result, settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process '{DamageBattleEffectSheetName}' from '{result.fileName ?? result.link}': {ex.Message}\n{ex}");
            }
        }
    }

    private static void ProcessDamageBattleEffectSheet(FileDownloader.DownloadResult result, DownloadSettings settings)
    {
        var delimiter = DetermineDelimiter(settings);
        if (!TryExtractSheet(result, DamageBattleEffectSheetName, delimiter, out var tableText) || string.IsNullOrEmpty(tableText))
        {
            return;
        }

        var sourceName = settings.delimiterMode == DelimiterMode.AutoFromExtension
            ? DamageBattleEffectSheetName + ".tsv"
            : DamageBattleEffectSheetName + ".txt";

        DamageBattleEffectsCreator.CreateFromText(tableText, settings, sourceName);
    }

    private static bool TryExtractSheet(FileDownloader.DownloadResult result, string sheetName, char delimiter, out string tableText)
    {
        tableText = null;
        var fileName = result.fileName ?? string.Empty;
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        if (IsSpreadsheetPayload(result, extension))
        {
            if (XlsxSheetReader.TryGetSheet(result.data, sheetName, out var rows) && rows != null && rows.Count > 0)
            {
                tableText = ConvertToDelimitedText(rows, delimiter);
                return !string.IsNullOrEmpty(tableText);
            }

            return false;
        }

        if (result.IsText && LooksLikeDamageBattleEffectText(result.data))
        {
            tableText = Encoding.UTF8.GetString(result.data);
            return !string.IsNullOrEmpty(tableText);
        }

        return false;
    }

    private static bool IsSpreadsheetPayload(FileDownloader.DownloadResult result, string extension)
    {
        if (extension == ".xlsx" || extension == ".xlsm")
        {
            return true;
        }

        var contentType = result.contentType?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(contentType) && contentType.Contains("spreadsheetml"))
        {
            return true;
        }

        var data = result.data;
        if (data != null && data.Length >= 4 && data[0] == 'P' && data[1] == 'K')
        {
            // XLSX files are zip archives starting with PK signature.
            return true;
        }

        return false;
    }

    private static bool LooksLikeDamageBattleEffectText(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return false;
        }

        var asText = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 512));
        if (string.IsNullOrEmpty(asText))
        {
            return false;
        }

        return asText.IndexOf("Damage", StringComparison.OrdinalIgnoreCase) >= 0 &&
               asText.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) >= 0 &&
               asText.IndexOf("MaxTick", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static char DetermineDelimiter(DownloadSettings settings)
    {
        switch (settings.delimiterMode)
        {
            case DelimiterMode.Comma:
                return ',';
            case DelimiterMode.Semicolon:
                return ';';
            case DelimiterMode.Tab:
                return '\t';
            case DelimiterMode.Custom:
                return !string.IsNullOrEmpty(settings.customDelimiter) ? settings.customDelimiter[0] : ',';
            case DelimiterMode.AutoFromExtension:
            default:
                return '\t';
        }
    }

    private static string ConvertToDelimitedText(List<List<string>> rows, char delimiter)
    {
        if (rows == null || rows.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null)
            {
                continue;
            }

            int cellCount = GetEffectiveCellCount(row);
            for (int c = 0; c < cellCount; c++)
            {
                if (c > 0)
                {
                    builder.Append(delimiter);
                }

                builder.Append(EscapeCell(row[c] ?? string.Empty, delimiter));
            }

            if (i < rows.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static int GetEffectiveCellCount(List<string> row)
    {
        int count = row.Count;
        while (count > 0)
        {
            var value = row[count - 1];
            if (!string.IsNullOrEmpty(value))
            {
                break;
            }

            count--;
        }

        return count;
    }

    private static string EscapeCell(string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.IndexOfAny(new[] { '\"', '\n', '\r', delimiter }) >= 0)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static class XlsxSheetReader
    {
        private static readonly XNamespace MainNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static bool TryGetSheet(byte[] data, string sheetName, out List<List<string>> rows)
        {
            rows = null;
            if (data == null || data.Length == 0 || string.IsNullOrEmpty(sheetName))
            {
                return false;
            }

            using var memory = new MemoryStream(data, false);
            using var archive = new ZipArchive(memory, ZipArchiveMode.Read, true);

            var workbookEntry = archive.GetEntry("xl/workbook.xml");
            if (workbookEntry == null)
            {
                return false;
            }

            var relationships = LoadRelationships(archive);
            var sheetTargets = LoadSheetTargets(workbookEntry, relationships);
            if (!sheetTargets.TryGetValue(sheetName, out var sheetPath))
            {
                return false;
            }

            var sharedStrings = LoadSharedStrings(archive);
            var sheetEntry = archive.GetEntry(sheetPath);
            if (sheetEntry == null)
            {
                return false;
            }

            rows = ParseWorksheet(sheetEntry, sharedStrings);
            return rows != null;
        }

        private static Dictionary<string, string> LoadRelationships(ZipArchive archive)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var relEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (relEntry == null)
            {
                return map;
            }

            using var relStream = relEntry.Open();
            var relDoc = XDocument.Load(relStream);
            foreach (var rel in relDoc.Root.Elements())
            {
                var id = (string)rel.Attribute("Id");
                var target = (string)rel.Attribute("Target");
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(target))
                {
                    continue;
                }

                map[id] = target.Replace('\\', '/');
            }

            return map;
        }

        private static Dictionary<string, string> LoadSheetTargets(ZipArchiveEntry workbookEntry, Dictionary<string, string> relationships)
        {
            using var stream = workbookEntry.Open();
            var doc = XDocument.Load(stream);
            var sheetTargets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sheetsElement = doc.Root.Element(MainNamespace + "sheets");
            if (sheetsElement == null)
            {
                return sheetTargets;
            }

            foreach (var sheet in sheetsElement.Elements(MainNamespace + "sheet"))
            {
                var name = (string)sheet.Attribute("name");
                var relId = (string)sheet.Attribute(RelationshipsNamespace + "id");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(relId))
                {
                    continue;
                }

                if (!relationships.TryGetValue(relId, out var target))
                {
                    continue;
                }

                sheetTargets[name] = NormalizeSheetPath(target);
            }

            return sheetTargets;
        }

        private static string NormalizeSheetPath(string target)
        {
            var normalized = target.Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = normalized.TrimStart('/');
            }

            var segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var resolved = new List<string>(segments.Length + 1);
            foreach (var segment in segments)
            {
                if (segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (resolved.Count > 0)
                    {
                        resolved.RemoveAt(resolved.Count - 1);
                    }

                    continue;
                }

                resolved.Add(segment);
            }

            var path = string.Join("/", resolved);
            if (!path.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            {
                path = "xl/" + path;
            }

            return path;
        }

        private static List<string> LoadSharedStrings(ZipArchive archive)
        {
            var shared = new List<string>();
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return shared;
            }

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            foreach (var si in doc.Root.Elements(MainNamespace + "si"))
            {
                var text = string.Concat(si.Descendants(MainNamespace + "t").Select(t => t.Value));
                shared.Add(text);
            }

            return shared;
        }

        private static List<List<string>> ParseWorksheet(ZipArchiveEntry sheetEntry, List<string> sharedStrings)
        {
            using var stream = sheetEntry.Open();
            var doc = XDocument.Load(stream);
            var sheetData = doc.Descendants(MainNamespace + "sheetData").FirstOrDefault();
            if (sheetData == null)
            {
                return null;
            }

            var rows = new List<List<string>>();
            foreach (var rowElement in sheetData.Elements(MainNamespace + "row"))
            {
                var row = new List<string>();
                foreach (var cell in rowElement.Elements(MainNamespace + "c"))
                {
                    var columnIndex = GetColumnIndex((string)cell.Attribute("r"));
                    EnsureCapacity(row, columnIndex + 1);
                    row[columnIndex] = ReadCellValue(cell, sharedStrings);
                }

                rows.Add(row);
            }

            return rows;
        }

        private static void EnsureCapacity(List<string> row, int size)
        {
            while (row.Count < size)
            {
                row.Add(string.Empty);
            }
        }

        private static int GetColumnIndex(string cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
            {
                return 0;
            }

            int index = 0;
            foreach (var ch in cellReference)
            {
                if (char.IsLetter(ch))
                {
                    index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
                }
                else
                {
                    break;
                }
            }

            return Math.Max(0, index - 1);
        }

        private static string ReadCellValue(XElement cell, List<string> sharedStrings)
        {
            var type = (string)cell.Attribute("t");
            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(MainNamespace + "t").Select(t => t.Value));
            }

            var valueElement = cell.Element(MainNamespace + "v");
            if (valueElement == null)
            {
                return string.Empty;
            }

            var valueText = valueElement.Value;
            if (type == "s")
            {
                if (int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex) &&
                    sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                {
                    return sharedStrings[sharedIndex];
                }

                return string.Empty;
            }

            return valueText ?? string.Empty;
        }
    }
}
#endif
