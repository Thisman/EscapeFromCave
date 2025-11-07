using System;
using System.Net.Http;
using UnityEngine;

public static class ImporterTableLoader
{
    private static readonly HttpClient _client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd("EntityImporter/1.0"))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("UnityEditor");
        }
        return client;
    }

    public static string Download(string url, string importerName, char delimiter)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning($"[{importerName}] TableUrl is empty");
            return null;
        }

        var resolvedUrl = url;
        try
        {
            resolvedUrl = PrepareUrl(url, delimiter);
            var text = _client.GetStringAsync(resolvedUrl).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(text) && text[0] == '\ufeff')
            {
                text = text.Substring(1);
            }
            return text;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{importerName}] Failed to download table from '{resolvedUrl}' (original '{url}'). {ex.Message}");
            return null;
        }
    }

    private static string PrepareUrl(string url, char delimiter)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        if (!uri.Host.Contains("docs.google.com", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (!uri.AbsolutePath.Contains("/spreadsheets/", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (uri.AbsolutePath.Contains("/export", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var path = uri.AbsolutePath.Trim('/').Split('/');
        int dIndex = Array.IndexOf(path, "d");
        if (dIndex < 0 || dIndex + 1 >= path.Length)
        {
            return url;
        }

        var docId = path[dIndex + 1];
        if (string.IsNullOrEmpty(docId))
        {
            return url;
        }

        string format = delimiter == '\t' ? "tsv" : "csv";
        string gid = ExtractGid(uri);

        var builder = new System.Text.StringBuilder();
        builder.Append("https://docs.google.com/spreadsheets/d/");
        builder.Append(docId);
        builder.Append("/export?format=");
        builder.Append(format);
        if (!string.IsNullOrEmpty(gid))
        {
            builder.Append("&gid=");
            builder.Append(gid);
        }

        return builder.ToString();
    }

    private static string ExtractGid(Uri uri)
    {
        string fragment = uri.Fragment;
        if (!string.IsNullOrEmpty(fragment) && fragment.StartsWith("#", StringComparison.Ordinal))
        {
            fragment = fragment.Substring(1);
        }

        string gid = TryGetQueryValue(fragment, "gid");
        if (!string.IsNullOrEmpty(gid))
        {
            return gid;
        }

        var query = uri.Query;
        if (!string.IsNullOrEmpty(query) && query.StartsWith("?", StringComparison.Ordinal))
        {
            query = query.Substring(1);
        }

        gid = TryGetQueryValue(query, "gid");
        return gid;
    }

    private static string TryGetQueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(kv[1]);
            }
        }

        return null;
    }
}
