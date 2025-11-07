using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Networking;

public static class FileDownloader
{
    public sealed class DownloadResult
    {
        public string link;           // исходная ссылка
        public string fileName;       // предполагаемое имя
        public string contentType;    // MIME, если известен
        public byte[] data;           // байты файла
        public string textPreview;    // превью как текст (если декодируемо)
        public string error;          // описание ошибки (если была)
        public bool IsText => !string.IsNullOrEmpty(textPreview);
        public int Size => data?.Length ?? 0;
    }

    public static async Task<List<DownloadResult>> DownloadAllAsync(DownloadSettings settings, IProgress<float> progress = null)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        var links = settings.links ?? Array.Empty<string>();
        var list = new List<DownloadResult>(links.Length);

        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (string.IsNullOrWhiteSpace(link))
            {
                list.Add(new DownloadResult { link = link, error = "Empty link" });
                progress?.Report((i + 1f) / Math.Max(1, links.Length));
                continue;
            }

            var res = new DownloadResult { link = link };
            try
            {
                if (IsHttp(link))
                {
                    await DownloadHttpAsync(link, res);
                }
                else if (IsFileUri(link))
                {
                    var p = new Uri(link).LocalPath;
                    LoadFromDisk(p, res);
                }
                else if (IsAssetsPath(link))
                {
#if UNITY_EDITOR
                    // Пытаемся как TextAsset (быстро и безопасно)
                    var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(link);
                    if (ta != null)
                    {
                        res.data = ta.bytes ?? System.Text.Encoding.UTF8.GetBytes(ta.text ?? "");
                        res.textPreview = ExtractPreview(res.data);
                        res.fileName = Path.GetFileName(link);
                        res.contentType = GuessContentType(res.fileName);
                    }
                    else
                    {
                        // как обычный файл
                        LoadFromDisk(Path.GetFullPath(link), res);
                    }
#else
                    throw new IOException("Assets path supported only in Editor.");
#endif
                }
#if UNITY_EDITOR
                else if (LooksLikeGuid(link))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(link);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                        if (ta != null)
                        {
                            res.data = ta.bytes ?? System.Text.Encoding.UTF8.GetBytes(ta.text ?? "");
                            res.textPreview = ExtractPreview(res.data);
                            res.fileName = Path.GetFileName(assetPath);
                            res.contentType = GuessContentType(res.fileName);
                        }
                        else
                        {
                            LoadFromDisk(Path.GetFullPath(assetPath), res);
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException("GUID not found: " + link);
                    }
                }
#endif
                else
                {
                    // абсолютный или относительный путь
                    LoadFromDisk(link, res);
                }
            }
            catch (Exception e)
            {
                res.error = e.Message;
            }

            list.Add(res);
            progress?.Report((i + 1f) / Math.Max(1, links.Length));
        }

        return list;
    }

    // ===== Helpers =====
    private static bool IsHttp(string s) =>
        s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static bool IsFileUri(string s) =>
        s.StartsWith("file://", StringComparison.OrdinalIgnoreCase);

    private static bool IsAssetsPath(string s) =>
        s.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase);

#if UNITY_EDITOR
    private static bool LooksLikeGuid(string s) =>
        s.Length == 32 && s.IndexOfAny(new[] { '/', '\\' }) < 0;
#endif

    private static async Task DownloadHttpAsync(string url, DownloadResult res)
    {
        using var req = UnityWebRequest.Get(url);
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
            throw new IOException(req.error);
#else
        if (req.isHttpError || req.isNetworkError)
            throw new IOException(req.error);
#endif

        res.data = req.downloadHandler.data;
        res.contentType = req.GetResponseHeader("Content-Type");
        res.fileName = TryGetFileNameFromHeadersOrUrl(url, req);
        res.textPreview = ExtractPreview(res.data);
    }

    private static void LoadFromDisk(string path, DownloadResult res)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);
        res.data = File.ReadAllBytes(path);
        res.fileName = Path.GetFileName(path);
        res.contentType = GuessContentType(res.fileName);
        res.textPreview = ExtractPreview(res.data);
    }

    private static string TryGetFileNameFromHeadersOrUrl(string url, UnityWebRequest req)
    {
        var disp = req.GetResponseHeader("Content-Disposition");
        if (!string.IsNullOrEmpty(disp))
        {
            // простой парсинг filename=
            const string key = "filename=";
            var idx = disp.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var name = disp.Substring(idx + key.Length).Trim(' ', '"', '\'', ';');
                if (!string.IsNullOrEmpty(name)) return name;
            }
        }
        // fallback: имя из URL
        try
        {
            var uri = new Uri(url);
            var name = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrEmpty(name)) return name;
        }
        catch { /* ignore */ }
        return "download";
    }

    private static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".csv" => "text/csv",
            ".tsv" => "text/tab-separated-values",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }

    private static string ExtractPreview(byte[] data)
    {
        if (data == null || data.Length == 0) return "";
        // Пытаемся декодировать как UTF-8 без BOM; если не похоже — оставим пустое превью
        try
        {
            var text = System.Text.Encoding.UTF8.GetString(data);
            // ограничим разумным объёмом, чтобы не подвесить инспектор
            const int max = 4 * 1024; // 4KB
            if (text.Length > max) text = text.Substring(0, max) + "\n...[truncated]";
            // простая эвристика «похоже на текст»
            var nonTextRatio = GetNonTextRatio(text);
            return nonTextRatio < 0.02 ? text : "";
        }
        catch { return ""; }
    }

    private static float GetNonTextRatio(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        int non = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            // разрешим печатные, таб/новую строку
            if (!char.IsWhiteSpace(c) && char.IsControl(c))
                non++;
        }
        return (float)non / s.Length;
    }
}
