using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EscapeFromCave.EditorTools.EntitiesImporter
{
    public class EntitiesImporter : EditorWindow
    {
        private EntitiesImporterSettingsSO settings;
        private Vector2 linksScrollPosition;
        private string statusMessage;
        private MessageType statusMessageType = MessageType.Info;

        [MenuItem("Tools/Entities Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<EntitiesImporter>();
            window.titleContent = new GUIContent("Entities Importer");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Entities Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            settings = (EntitiesImporterSettingsSO)EditorGUILayout.ObjectField("Settings", settings, typeof(EntitiesImporterSettingsSO), false);

            using (new EditorGUI.DisabledScope(settings == null))
            {
                if (settings != null)
                {
                    DrawSettingsPreview();
                }

                if (GUILayout.Button("Import"))
                {
                    TryImport();
                }
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }
        }

        private void DrawSettingsPreview()
        {
            EditorGUILayout.LabelField("Delimiter", settings.Delimiter.ToString());

            var links = settings.TableLinks;
            if (links == null || links.Count == 0)
            {
                EditorGUILayout.HelpBox("No table links specified in the provided settings asset.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Table Links:");
            linksScrollPosition = EditorGUILayout.BeginScrollView(linksScrollPosition, GUILayout.Height(100f));
            for (var i = 0; i < links.Count; i++)
            {
                EditorGUILayout.LabelField($"{i + 1}. {links[i]}");
            }

            EditorGUILayout.EndScrollView();
        }

        private void TryImport()
        {
            try
            {
                var importedSheets = EntitiesSpreadsheetLoader.Import(settings);

                var builder = new StringBuilder();
                builder.AppendLine($"Imported {importedSheets.Count} sheet(s).");
                foreach (var sheet in importedSheets)
                {
                    builder.AppendLine($"- {sheet.TableTitle}/{sheet.SheetTitle}: {sheet.Rows.Count} row(s)");
                }

                statusMessage = builder.ToString();
                statusMessageType = MessageType.Info;
                Debug.Log(statusMessage);
            }
            catch (Exception exception)
            {
                statusMessage = exception.Message;
                statusMessageType = MessageType.Error;
                Debug.LogException(exception);
            }
        }
    }
}
