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

    public static string Download(string url, string importerName)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning($"[{importerName}] TableUrl is empty");
            return null;
        }

        try
        {
            var text = _client.GetStringAsync(url).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(text) && text[0] == '\ufeff')
            {
                text = text.Substring(1);
            }
            return text;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{importerName}] Failed to download table from '{url}'. {ex.Message}");
            return null;
        }
    }
}
