using System.IO;
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

            var nonEmptyRowCount = CountNonEmptyRows(table.text);
            Debug.Log($"[EntitiesImporter] Sheet '{table.name}' has {nonEmptyRowCount} non-empty rows.");
        }
    }

    private static int CountNonEmptyRows(string csvContent)
    {
        var nonEmptyRowCount = 0;
        using (var reader = new StringReader(csvContent))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    nonEmptyRowCount++;
                }
            }
        }

        return nonEmptyRowCount;
    }
}
