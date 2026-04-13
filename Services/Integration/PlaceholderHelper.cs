using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TMSBilling.Services.Integration
{
    /// <summary>
    /// Resolves {{placeholder}} in templates from event data.
    /// Built-in placeholders: {{date}}, {{datetime}}, {{timestamp}}
    /// Custom placeholders: resolved from eventData keys (case-insensitive)
    /// </summary>
    public static class PlaceholderHelper
    {
        public static string Resolve(string template, Dictionary<string, object?> eventData)
        {
            if (string.IsNullOrWhiteSpace(template)) return template;

            return Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
            {
                var key = match.Groups[1].Value.ToLower();

                return key switch
                {
                    "date"      => DateTime.Now.ToString("yyyy-MM-dd"),
                    "filedatetime" => DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt"),
                    "datetime"  => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "timestamp" => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    "year"      => DateTime.Now.Year.ToString(),
                    "month"     => DateTime.Now.Month.ToString("D2"),
                    "day"       => DateTime.Now.Day.ToString("D2"),
                    _ => ResolveFromData(key, eventData)
                };
            });
        }

        private static string ResolveFromData(string key, Dictionary<string, object?> data)
        {
            foreach (var (k, v) in data)
            {
                if (k.ToLower() == key || k.ToLower().Replace(" ", "_") == key)
                    return v?.ToString() ?? string.Empty;
            }
            return $"{{{{{key}}}}}"; // leave as-is if not found
        }
    }
}
