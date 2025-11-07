#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public sealed class FileDownloaderWindow : EditorWindow
{
    private DownloadSettings _settings;
    private bool _running;
    private float _progress;
    private Vector2 _scroll;
    private List<FileDownloader.DownloadResult> _results;

    [MenuItem("Tools/Tables/File Downloader")]
    public static void Open()
    {
        GetWindow<FileDownloaderWindow>("File Downloader");
    }

    private void OnGUI()
    {
        using (new EditorGUI.DisabledScope(_running))
        {
            _settings = (DownloadSettings)EditorGUILayout.ObjectField("Settings", _settings, typeof(DownloadSettings), false);

            if (_settings == null)
            {
                EditorGUILayout.HelpBox("Укажите DownloadSettings (.asset). Создать: Assets > Create > Tables > Download Settings", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button("Download"))
                    _ = RunAsync();
            }
        }

        if (_running)
        {
            var r = GUILayoutUtility.GetRect(18, 18);
            EditorGUI.ProgressBar(r, _progress, $"Downloading... {Mathf.RoundToInt(_progress * 100)}%");
            Repaint();
        }

        if (_results != null)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Results: {_results.Count}", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var r in _results)
            {
                DrawResult(r);
                EditorGUILayout.Space(6);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawResult(FileDownloader.DownloadResult r)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Link", r.link);
            if (!string.IsNullOrEmpty(r.error))
            {
                EditorGUILayout.HelpBox(r.error, MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("File Name", r.fileName ?? "(unknown)");
            EditorGUILayout.LabelField("Content-Type", r.contentType ?? "(unknown)");
            EditorGUILayout.LabelField("Size", r.Size.ToString("N0") + " bytes");

            if (r.IsText)
            {
                EditorGUILayout.LabelField("Preview (UTF-8):");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextArea(r.textPreview, GUILayout.MinHeight(80));
                }

                if (GUILayout.Button("Copy preview to clipboard"))
                    EditorGUIUtility.systemCopyBuffer = r.textPreview ?? "";
            }
            else
            {
                EditorGUILayout.LabelField("Preview", "(binary / not a UTF-8 text)");
            }
        }
    }

    private async Task RunAsync()
    {
        if (_settings == null) return;

        _running = true;
        _progress = 0f;
        _results = null;

        try
        {
            var prog = new Progress<float>(p =>
            {
                _progress = p;
                EditorUtility.DisplayProgressBar("Downloading", "Fetching files...", p);
            });

            _results = await FileDownloader.DownloadAllAsync(_settings, prog);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            _running = false;
            _progress = 1f;
            Repaint();
        }
    }
}
#endif
