using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public class EntitiesImporter : EditorWindow
{
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
                foreach (var sheetInfo in ReadSheets(absolutePath))
                {
                    Debug.Log($"[EntitiesImporter] Sheet '{sheetInfo.Name}' has {sheetInfo.RowCount} rows.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EntitiesImporter] Failed to read '{table.name}': {exception.Message}");
            }
        }
    }

    private static IEnumerable<(string Name, int RowCount)> ReadSheets(string xlsxPath)
    {
        using var fileStream = File.OpenRead(xlsxPath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);

        var workbookEntry = archive.GetEntry("xl/workbook.xml") ??
                             throw new InvalidDataException("Workbook definition is missing.");
        var relationshipEntry = archive.GetEntry("xl/_rels/workbook.xml.rels") ??
                                 throw new InvalidDataException("Workbook relationships are missing.");

        var sheetDefinitions = LoadSheetDefinitions(workbookEntry);
        var relationships = LoadRelationships(relationshipEntry);

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

            var rowCount = CountRows(worksheetEntry);
            yield return (sheet.Name, rowCount);
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
                Name: (string?)sheet.Attribute("name") ?? "Unnamed",
                RelationshipId: (string?)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")) ?? string.Empty))
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
                Id = (string?)relationship.Attribute("Id"),
                Target = (string?)relationship.Attribute("Target")
            })
            .Where(data => !string.IsNullOrEmpty(data.Id) && !string.IsNullOrEmpty(data.Target))
            .ToDictionary(
                data => data.Id!,
                data => NormalizeEntryPath(data.Target!));
    }

    private static int CountRows(ZipArchiveEntry worksheetEntry)
    {
        using var stream = worksheetEntry.Open();
        var document = XDocument.Load(stream);
        var mainNamespace = document.Root?.Name.Namespace ?? XNamespace.None;
        var sheetData = document.Root?.Element(mainNamespace + "sheetData");
        return sheetData?.Elements(mainNamespace + "row").Count() ?? 0;
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
