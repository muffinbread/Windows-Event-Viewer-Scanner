using System.Text.RegularExpressions;
using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Best-effort extraction of IPv4s, paths, account names, etc. from event messages.
/// Results are hints only — messages are unstructured and may not describe the whole story.
/// </summary>
public static class EventMessageEntityExtractor
{
    private static readonly Regex Ipv4Regex = new(
        @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b",
        RegexOptions.Compiled);

    private static readonly Regex UncPathRegex = new(
        @"\\\\[^\s<>""|?*,]+(?:\\[^\s<>""|?*,]+)+",
        RegexOptions.Compiled);

    private static readonly Regex DrivePathRegex = new(
        @"(?:[A-Za-z]:\\)(?:[^\\/:*?""<>|\r\n,]+\\)*[^\\/:*?""<>|\r\n,]+",
        RegexOptions.Compiled);

    /// <summary>
    /// DOMAIN\user or COMPUTER\user style (requires backslash, not an IPv4).
    /// </summary>
    private static readonly Regex AccountRegex = new(
        @"\b(?![0-9]{1,3}\.[0-9]{1,3}\.)[A-Za-z0-9][A-Za-z0-9._-]{0,63}\\[A-Za-z0-9$][A-Za-z0-9._$-]{0,63}\b",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);

    public static IReadOnlyList<ExtractedPivot> ExtractFromFinding(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<ExtractedPivot>();

        foreach (var ev in finding.MatchedEvents)
        {
            if (!string.IsNullOrWhiteSpace(ev.MachineName))
                TryAdd("Computer", ev.MachineName.Trim(), seen, results);

            ExtractFromText(ev.Message ?? "", seen, results);
        }

        return SortResults(results);
    }

    private static void ExtractFromText(string text, HashSet<string> seen, List<ExtractedPivot> results)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (Match m in Ipv4Regex.Matches(text))
            TryAdd("IPv4", m.Value, seen, results);

        foreach (Match m in AccountRegex.Matches(text))
        {
            var raw = m.Value;
            var parts = raw.Split('\\', 2);
            if (parts.Length == 2 && LooksLikeFileBackedSecondPart(parts[1]))
                continue;
            TryAdd("Account", raw, seen, results);
        }

        foreach (Match m in EmailRegex.Matches(text))
            TryAdd("Email", m.Value, seen, results);

        foreach (Match m in UncPathRegex.Matches(text))
            TryAdd("Path", TrimTrailingJunk(m.Value), seen, results);

        foreach (Match m in DrivePathRegex.Matches(text))
            TryAdd("Path", TrimTrailingJunk(m.Value), seen, results);
    }

    private static void TryAdd(string category, string value, HashSet<string> seen, List<ExtractedPivot> results)
    {
        value = value.Trim();
        if (value.Length == 0)
            return;

        var key = category + '\u001f' + value;
        if (!seen.Add(key))
            return;

        results.Add(new ExtractedPivot(category, value));
    }

    private static string TrimTrailingJunk(string path)
    {
        path = path.TrimEnd();
        while (path.Length > 0 && IsTrailingJunk(path[^1]))
            path = path[..^1];
        return path;
    }

    private static bool IsTrailingJunk(char c) =>
        c is '.' or ',' or ';' or ':' or ')' or ']' or '}' or '"' or '\'';

    /// <summary>
    /// Drop DOMAIN\file.exe style false positives from the account pattern matching path fragments.
    /// </summary>
    private static bool LooksLikeFileBackedSecondPart(string secondSegment)
    {
        var idx = secondSegment.LastIndexOf('.');
        if (idx <= 0 || idx == secondSegment.Length - 1)
            return false;
        var ext = secondSegment[(idx + 1)..].ToLowerInvariant();
        return ext is "exe" or "dll" or "bat" or "cmd" or "ps1" or "msi" or "scr" or "com";
    }

    private static IReadOnlyList<ExtractedPivot> SortResults(List<ExtractedPivot> results)
    {
        return results
            .OrderBy(p => CategoryOrder(p.Category))
            .ThenBy(p => p.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int CategoryOrder(string c) =>
        c switch
        {
            "IPv4" => 0,
            "Account" => 1,
            "Email" => 2,
            "Path" => 3,
            "Computer" => 4,
            _ => 99
        };
}
