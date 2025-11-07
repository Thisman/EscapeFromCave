#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public sealed class FileDownloaderWindow : EditorWindow
{
    private DownloadSettings _settings;
    private bool _running;
    private float _progress;

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

    }

    private async Task RunAsync()
    {
        if (_settings == null || _running) return;

        _running = true;
        _progress = 0f;

        var prog = new Progress<float>(p =>
        {
            _progress = p;
            Repaint();
        });

        var settingsSnapshot = _settings;

        try
        {
            var results = await FileDownloader.DownloadAllAsync(settingsSnapshot, prog);

            ScheduleOnMainThread(() =>
            {
                try
                {
                    DownloadedTablesProcessor.Process(results, settingsSnapshot);
                }
                finally
                {
                    _running = false;
                    _progress = 1f;
                    Repaint();
                }
            });
        }
        catch (Exception ex)
        {
            ScheduleOnMainThread(() =>
            {
                Debug.LogError(ex);
                _running = false;
                Repaint();
            });
        }
    }

    private static void ScheduleOnMainThread(Action action)
    {
        if (action == null)
        {
            return;
        }

        EditorApplication.delayCall += Invoke;

        void Invoke()
        {
            EditorApplication.delayCall -= Invoke;
            action();
        }
    }
}
#endif
