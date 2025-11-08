using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public interface IEntitiesSheetImporter
{
    string SheetName { get; }

    void Import(EntitiesSheet sheet, EntitiesImporterSettings settings);
}

public sealed class EntitiesSheet
{
    public EntitiesSheet(string name, IReadOnlyList<string> headers, IReadOnlyList<EntitiesSheetRow> rows)
    {
        Name = name;
        Headers = headers;
        Rows = rows;
    }

    public string Name { get; }

    public IReadOnlyList<string> Headers { get; }

    public IReadOnlyList<EntitiesSheetRow> Rows { get; }
}

public sealed class EntitiesSheetRow
{
    private readonly Dictionary<string, string> values;

    public EntitiesSheetRow(int rowNumber, Dictionary<string, string> values)
    {
        RowNumber = rowNumber;
        this.values = values;
    }

    public int RowNumber { get; }

    public IReadOnlyDictionary<string, string> Values => values;

    public bool TryGetValue(string header, out string value)
    {
        return values.TryGetValue(header, out value);
    }

    public string GetValueOrDefault(string header)
    {
        return values.TryGetValue(header, out var value) ? value : string.Empty;
    }
}

public class EntitiesImporter : EditorWindow
{
    private static readonly IEntitiesSheetImporter[] SheetImporters =
    {
        new DamageBattleEffectImporter(),
        new StatModifierBattleEffectImporter(),
    };

    private EntitiesImporterSettings settings;

    [MenuItem("Tools/Entities Importer")]
    public static void ShowWindow()
    {
        GetWindow<EntitiesImporter>("Entities Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Entities Importer", EditorStyles.boldLabel);
        settings = (EntitiesImporterSettings)EditorGUILayout.ObjectField("Settings", settings, typeof(EntitiesImporterSettings), false);

        using (new EditorGUI.DisabledScope(settings == null))
        {
            if (GUILayout.Button("Import"))
            {
                Import();
            }
        }
    }

    private void Import()
    {
        if (settings == null)
        {
            Debug.LogWarning("[EntitiesImporter] Settings asset is not assigned.");
            return;
        }

        foreach (var table in settings.Tables)
        {
            if (table == null)
            {
                continue;
            }

            var assetPath = AssetDatabase.GetAssetPath(table);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"[EntitiesImporter] Unable to resolve path for asset '{table.name}'.");
                continue;
            }

            var absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"[EntitiesImporter] File not found at '{absolutePath}'.");
                continue;
            }

            try
            {
                foreach (var sheet in ReadSheets(absolutePath))
                {
                    Debug.Log($"[EntitiesImporter] Sheet '{sheet.Name}' has {sheet.Rows.Count} data rows.");

                    var importer = FindImporterForSheet(sheet.Name);
                    if (importer == null)
                    {
                        continue;
                    }

                    try
                    {
                        importer.Import(sheet, settings);
                    }
                    catch (Exception importerException)
                    {
                        Debug.LogError($"[EntitiesImporter] Importer '{importer.SheetName}' failed: {importerException.Message}");
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EntitiesImporter] Failed to read '{table.name}': {exception.Message}");
            }
        }
    }

    private static IEntitiesSheetImporter FindImporterForSheet(string sheetName)
    {
        return SheetImporters.FirstOrDefault(importer =>
            string.Equals(importer.SheetName, sheetName, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<EntitiesSheet> ReadSheets(string xlsxPath)
    {
        using var fileStream = File.OpenRead(xlsxPath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);

        var workbookEntry = archive.GetEntry("xl/workbook.xml") ??
                             throw new InvalidDataException("Workbook definition is missing.");
        var relationshipEntry = archive.GetEntry("xl/_rels/workbook.xml.rels") ??
                                 throw new InvalidDataException("Workbook relationships are missing.");

        var sheetDefinitions = LoadSheetDefinitions(workbookEntry);
        var relationships = LoadRelationships(relationshipEntry);
        var sharedStrings = LoadSharedStrings(archive);

        foreach (var sheet in sheetDefinitions)
        {
            if (!relationships.TryGetValue(sheet.RelationshipId, out var targetPath))
            {
                Debug.LogWarning($"[EntitiesImporter] Relationship '{sheet.RelationshipId}' not found for sheet '{sheet.Name}'.");
                continue;
            }

            var worksheetEntry = archive.GetEntry(targetPath);
            if (worksheetEntry == null)
            {
                Debug.LogWarning($"[EntitiesImporter] Worksheet '{targetPath}' not found for sheet '{sheet.Name}'.");
                continue;
            }

            var sheetData = LoadWorksheet(worksheetEntry, sheet.Name, sharedStrings);
            if (sheetData != null)
            {
                yield return sheetData;
            }
        }
    }

    private static IReadOnlyList<(string Name, string RelationshipId)> LoadSheetDefinitions(ZipArchiveEntry workbookEntry)
    {
        using var stream = workbookEntry.Open();
        var document = XDocument.Load(stream);
        var mainNamespace = document.Root?.Name.Namespace ?? XNamespace.None;

        return document
            .Descendants(mainNamespace + "sheet")
            .Select(sheet => (
                Name: (string)sheet.Attribute("name") ?? "Unnamed",
                RelationshipId: (string)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")) ?? string.Empty))
            .Where(sheet => !string.IsNullOrEmpty(sheet.RelationshipId))
            .ToList();
    }

    private static Dictionary<string, string> LoadRelationships(ZipArchiveEntry relationshipEntry)
    {
        using var stream = relationshipEntry.Open();
        var document = XDocument.Load(stream);
        var relationshipsNamespace = document.Root?.Name.Namespace ?? XNamespace.None;

        return document
            .Descendants(relationshipsNamespace + "Relationship")
            .Select(relationship => new
            {
                Id = (string)relationship.Attribute("Id"),
                Target = (string)relationship.Attribute("Target")
            })
            .Where(data => !string.IsNullOrEmpty(data.Id) && !string.IsNullOrEmpty(data.Target))
            .ToDictionary(
                data => data.Id!,
                data => NormalizeEntryPath(data.Target!));
    }

    private static IReadOnlyList<string> LoadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
        {
            return Array.Empty<string>();
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        var mainNamespace = document.Root?.Name.Namespace ?? XNamespace.None;

        return document
            .Descendants(mainNamespace + "si")
            .Select(si => string.Concat(si
                .Descendants(mainNamespace + "t")
                .Select(textNode => textNode.Value)))
            .ToList();
    }

    private static EntitiesSheet LoadWorksheet(ZipArchiveEntry worksheetEntry, string sheetName, IReadOnlyList<string> sharedStrings)
    {
        using var stream = worksheetEntry.Open();
        var document = XDocument.Load(stream);
        var mainNamespace = document.Root?.Name.Namespace ?? XNamespace.None;
        var sheetDataElement = document.Root?.Element(mainNamespace + "sheetData");
        if (sheetDataElement == null)
        {
            return new EntitiesSheet(sheetName, Array.Empty<string>(), Array.Empty<EntitiesSheetRow>());
        }

        var headers = new List<string>();
        var rows = new List<EntitiesSheetRow>();
        var headerInitialized = false;

        foreach (var rowElement in sheetDataElement.Elements(mainNamespace + "row"))
        {
            var cells = ReadRowCells(rowElement, sharedStrings);

            if (!headerInitialized)
            {
                for (var i = 0; i < cells.Count; i++)
                {
                    var headerValue = cells[i];
                    headers.Add(string.IsNullOrWhiteSpace(headerValue) ? $"Column{i + 1}" : headerValue);
                }

                headerInitialized = true;
                continue;
            }

            while (cells.Count > headers.Count)
            {
                headers.Add($"Column{headers.Count + 1}");
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var value = i < cells.Count ? cells[i] : string.Empty;
                values[headers[i]] = value;
            }

            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var rowReference = (string)rowElement.Attribute("r");
            int rowNumber;
            if (!string.IsNullOrEmpty(rowReference) && int.TryParse(rowReference, out var parsedRowNumber))
            {
                rowNumber = parsedRowNumber;
            }
            else
            {
                rowNumber = rows.Count + 2;
            }

            rows.Add(new EntitiesSheetRow(rowNumber, values));
        }

        return new EntitiesSheet(sheetName, headers, rows);
    }

    private static List<string> ReadRowCells(XElement rowElement, IReadOnlyList<string> sharedStrings)
    {
        var result = new List<string>();
        foreach (var cellElement in rowElement.Elements())
        {
            if (cellElement.Name.LocalName != "c")
            {
                continue;
            }

            var reference = (string)cellElement.Attribute("r");
            var columnIndex = !string.IsNullOrEmpty(reference) ? GetColumnIndex(reference) : result.Count;

            while (result.Count < columnIndex)
            {
                result.Add(string.Empty);
            }

            var value = ReadCellValue(cellElement, sharedStrings);
            if (result.Count == columnIndex)
            {
                result.Add(value);
            }
            else
            {
                result[columnIndex] = value;
            }
        }

        return result;
    }

    private static string ReadCellValue(XElement cellElement, IReadOnlyList<string> sharedStrings)
    {
        var cellType = (string)cellElement.Attribute("t");
        var valueElement = cellElement.Elements().FirstOrDefault(e => e.Name.LocalName == "v");

        switch (cellType)
        {
            case "s":
                if (valueElement == null)
                {
                    return string.Empty;
                }

                if (int.TryParse(valueElement.Value, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                {
                    return sharedStrings[sharedIndex];
                }

                return string.Empty;
            case "inlineStr":
                var inline = cellElement
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "is");
                if (inline != null)
                {
                    return string.Concat(inline
                        .Descendants()
                        .Where(e => e.Name.LocalName == "t")
                        .Select(textNode => textNode.Value));
                }

                return valueElement?.Value ?? string.Empty;
            case "str":
                return valueElement?.Value ?? string.Empty;
            default:
                return valueElement?.Value ?? string.Empty;
        }
    }

    private static int GetColumnIndex(string cellReference)
    {
        var index = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return Math.Max(0, index - 1);
    }

    private static string NormalizeEntryPath(string relativePath)
    {
        var sanitized = relativePath.Replace('\\', '/');

        while (sanitized.StartsWith("../", StringComparison.Ordinal))
        {
            sanitized = sanitized.Substring(3);
        }

        if (sanitized.StartsWith("./", StringComparison.Ordinal))
        {
            sanitized = sanitized.Substring(2);
        }

        sanitized = sanitized.TrimStart('/');

        if (!sanitized.StartsWith("xl/", StringComparison.Ordinal))
        {
            sanitized = $"xl/{sanitized}";
        }

        return sanitized;
    }
}
