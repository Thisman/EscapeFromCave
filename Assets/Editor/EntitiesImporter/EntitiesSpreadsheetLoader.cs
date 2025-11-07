using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace EscapeFromCave.EditorTools.EntitiesImporter
{
    internal static class EntitiesSpreadsheetLoader
    {
        private const string WorksheetListUrl = "https://spreadsheets.google.com/feeds/worksheets/{0}/public/full?alt=json";
        private const string SheetExportUrl = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}";

        public static List<ImportedSheet> Import(EntitiesImporterSettingsSO settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var importedSheets = new List<ImportedSheet>();
            foreach (var link in settings.TableLinks)
            {
                if (string.IsNullOrWhiteSpace(link))
                {
                    continue;
                }

                var spreadsheetId = ExtractSpreadsheetId(link);
                if (string.IsNullOrEmpty(spreadsheetId))
                {
                    throw new InvalidOperationException($"Failed to parse spreadsheet identifier from link: {link}");
                }

                var worksheetList = DownloadString(string.Format(WorksheetListUrl, spreadsheetId));
                var tableTitle = ExtractSpreadsheetTitle(worksheetList, spreadsheetId);
                var sheets = ExtractWorksheetInfos(worksheetList);
                if (sheets.Count == 0)
                {
                    Debug.LogWarning($"No public sheets found for spreadsheet '{spreadsheetId}'.");
                    continue;
                }

                foreach (var sheet in sheets)
                {
                    var csv = DownloadString(string.Format(SheetExportUrl, spreadsheetId, sheet.Gid));
                    var rows = ParseCsv(csv, settings.Delimiter);
                    importedSheets.Add(new ImportedSheet(tableTitle, sheet.Title, rows));
                }
            }

            return importedSheets;
        }

        private static string ExtractSpreadsheetId(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return string.Empty;
            }

            var match = Regex.Match(link, @"/d/([a-zA-Z0-9-_]+)");
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            match = Regex.Match(link, @"key=([a-zA-Z0-9-_]+)");
            return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : string.Empty;
        }

        private static string ExtractSpreadsheetTitle(string worksheetFeed, string fallback)
        {
            if (string.IsNullOrEmpty(worksheetFeed))
            {
                return fallback;
            }

            var match = Regex.Match(worksheetFeed, @"\"title\"\s*:\s*\{\s*\"type\"\s*:\s*\"text\"\s*,\s*\"\\$t\"\s*:\s*\"(?<title>[^\"]+)\"", RegexOptions.Singleline);
            return match.Success ? match.Groups["title"].Value : fallback;
        }

        private static List<WorksheetInfo> ExtractWorksheetInfos(string worksheetFeed)
        {
            var results = new List<WorksheetInfo>();
            if (string.IsNullOrEmpty(worksheetFeed))
            {
                return results;
            }

            var entryRegex = new Regex(@"\{[^{}]*\"gs\\$gid\"\s*:\s*\"(?<gid>[^\"]+)\"[^{}]*\"title\"\s*:\s*\{\s*\"\\$t\"\s*:\s*\"(?<title>[^\"]+)\"", RegexOptions.Singleline);
            var matches = entryRegex.Matches(worksheetFeed);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var gid = match.Groups["gid"].Value;
                var title = match.Groups["title"].Value;

                if (string.IsNullOrEmpty(gid) || string.IsNullOrEmpty(title))
                {
                    continue;
                }

                results.Add(new WorksheetInfo(title, gid));
            }

            return results;
        }

        private static string DownloadString(string url)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    Thread.Sleep(1);
                }

#if UNITY_2020_2_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    throw new InvalidOperationException($"Failed to download data from '{url}': {request.error}");
                }

                return request.downloadHandler.text;
            }
        }

        private static List<string[]> ParseCsv(string csv, char delimiter)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(csv))
            {
                return rows;
            }

            using (var reader = new StringReader(csv))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    rows.Add(ParseCsvLine(line, delimiter));
                }
            }

            return rows;
        }

        private static string[] ParseCsvLine(string line, char delimiter)
        {
            var values = new List<string>();
            if (line == null)
            {
                return values.ToArray();
            }

            var builder = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        builder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    values.Add(builder.ToString());
                    builder.Length = 0;
                }
                else
                {
                    builder.Append(c);
                }
            }

            values.Add(builder.ToString());
            return values.ToArray();
        }

        internal readonly struct WorksheetInfo
        {
            public WorksheetInfo(string title, string gid)
            {
                Title = title;
                Gid = gid;
            }

            public string Title { get; }
            public string Gid { get; }
        }

        public sealed class ImportedSheet
        {
            public ImportedSheet(string tableTitle, string sheetTitle, List<string[]> rows)
            {
                TableTitle = tableTitle;
                SheetTitle = sheetTitle;
                Rows = rows ?? new List<string[]>();
            }

            public string TableTitle { get; }
            public string SheetTitle { get; }
            public List<string[]> Rows { get; }
        }
    }
}
