using System.Text;
using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Generates a self-contained HTML report from a scan result.
/// The report includes inline CSS so it works as a single file
/// opened in any browser — no external dependencies needed.
/// </summary>
public static class ReportExporter
{
    public static string GenerateHtml(ScanResult result)
    {
        var sb = new StringBuilder();
        var gradeColor = GetGradeColorHex(result.Grade.Grade);
        var generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");

        AppendHead(sb);
        sb.AppendLine("<body>");
        AppendHeader(sb, result, gradeColor, generatedAt);
        AppendSummary(sb, result);
        AppendFindings(sb, result);
        AppendFooter(sb, generatedAt);
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void AppendHead(StringBuilder sb)
    {
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<title>EventScanner Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(CssStyles);
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
    }

    private static void AppendHeader(StringBuilder sb, ScanResult result, string gradeColor, string generatedAt)
    {
        sb.AppendLine("<header>");
        sb.AppendLine("  <h1>EventScanner Report</h1>");
        sb.AppendLine($"  <div class=\"grade\" style=\"color:{gradeColor}; text-shadow: 0 0 30px {gradeColor}, 0 0 60px {gradeColor}40;\">{Encode(result.Grade.DisplayName)}</div>");
        sb.AppendLine($"  <div class=\"score\">Score: {result.Grade.Score} / 100</div>");
        sb.AppendLine($"  <div class=\"description\">{Encode(result.Grade.Description)}</div>");
        sb.AppendLine("</header>");
    }

    private static void AppendSummary(StringBuilder sb, ScanResult result)
    {
        var scanTypeLabel = result.ScanType switch
        {
            ScanType.Quick => "Quick Scan (24 hours)",
            ScanType.Deep => "Deep Scan (30 days)",
            ScanType.Import when result.AnalysisProfile == ScanAnalysisProfile.CtfSpeed =>
                "Imported log files (CTF triage — same rules; findings sorted for fast review)",
            ScanType.Import => "Imported Log Files",
            _ => "Scan"
        };

        sb.AppendLine("<section class=\"summary\">");
        sb.AppendLine("  <h2>Scan Summary</h2>");
        sb.AppendLine("  <div class=\"summary-grid\">");
        sb.AppendLine($"    <div class=\"stat\"><span class=\"label\">Scan Type</span><span class=\"value\">{scanTypeLabel}</span></div>");
        sb.AppendLine($"    <div class=\"stat\"><span class=\"label\">Events Analyzed</span><span class=\"value\">{result.TotalEventsAnalyzed:N0}</span></div>");
        sb.AppendLine($"    <div class=\"stat\"><span class=\"label\">Findings</span><span class=\"value\">{result.Findings.Count}</span></div>");
        sb.AppendLine($"    <div class=\"stat\"><span class=\"label\">Duration</span><span class=\"value\">{result.Duration.TotalSeconds:F1}s</span></div>");
        sb.AppendLine($"    <div class=\"stat\"><span class=\"label\">Logs Scanned</span><span class=\"value\">{string.Join(", ", result.ScannedLogs)}</span></div>");
        sb.AppendLine($"    <div class=\"stat\"><span class=\"label\">Time Range</span><span class=\"value\">{result.ScanRangeStart:yyyy-MM-dd} to {result.ScanRangeEnd:yyyy-MM-dd}</span></div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</section>");
    }

    private static void AppendFindings(StringBuilder sb, ScanResult result)
    {
        sb.AppendLine("<section class=\"findings\">");
        sb.AppendLine("  <h2>Findings</h2>");

        if (result.Findings.Count == 0)
        {
            sb.AppendLine("  <p class=\"no-findings\">No issues found — your system looks great!</p>");
            sb.AppendLine("</section>");
            return;
        }

        foreach (var finding in result.Findings)
        {
            var sevColor = GetSeverityColorHex(finding.Severity);
            var sevBg = GetSeverityBgHex(finding.Severity);

            sb.AppendLine("  <div class=\"finding\">");
            sb.AppendLine("    <div class=\"finding-header\">");
            sb.AppendLine($"      <span class=\"severity-badge\" style=\"color:{sevColor};background:{sevBg};\">{finding.Severity}</span>");
            sb.AppendLine($"      <span class=\"finding-title\">{Encode(finding.Title)}</span>");
            sb.AppendLine($"      <span class=\"finding-meta\">{Encode(finding.Category)} &middot; {finding.OccurrenceCount}x</span>");
            sb.AppendLine("    </div>");
            sb.AppendLine($"    <p class=\"finding-desc\">{Encode(finding.Description)}</p>");

            if (!string.IsNullOrWhiteSpace(finding.Explanation))
            {
                sb.AppendLine($"    <div class=\"detail-section\"><strong>Explanation:</strong> {Encode(finding.Explanation)}</div>");
            }

            if (!string.IsNullOrWhiteSpace(finding.WhyItMatters))
            {
                sb.AppendLine($"    <div class=\"detail-section\"><strong>Why It Matters:</strong> {Encode(finding.WhyItMatters)}</div>");
            }

            if (finding.PossibleCauses.Count > 0)
            {
                sb.AppendLine("    <div class=\"detail-section\"><strong>Possible Causes:</strong><ul>");
                foreach (var cause in finding.PossibleCauses)
                    sb.AppendLine($"      <li>{Encode(cause)}</li>");
                sb.AppendLine("    </ul></div>");
            }

            if (result.AnalysisProfile == ScanAnalysisProfile.CtfSpeed)
                AppendCtfPivotsForFinding(sb, finding);

            if (finding.MatchedEvents.Count > 0)
            {
                sb.AppendLine($"    <details><summary>Event Timeline ({finding.MatchedEvents.Count} events)</summary>");
                sb.AppendLine("    <table class=\"events-table\">");
                sb.AppendLine("      <thead><tr><th>Timestamp</th><th>ID</th><th>Level</th><th>Provider</th><th>Message</th></tr></thead>");
                sb.AppendLine("      <tbody>");
                foreach (var evt in finding.MatchedEvents)
                {
                    var time = evt.TimeCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
                    var msg = evt.Message.Length > 200 ? evt.Message[..200] + "..." : evt.Message;
                    sb.AppendLine($"      <tr><td>{time}</td><td>{evt.EventId}</td><td>{evt.Level}</td><td>{Encode(evt.ProviderName)}</td><td>{Encode(msg)}</td></tr>");
                }
                sb.AppendLine("      </tbody></table>");
                sb.AppendLine("    </details>");
            }

            sb.AppendLine("  </div>");
        }

        sb.AppendLine("</section>");
    }

    private static void AppendCtfPivotsForFinding(StringBuilder sb, Finding finding)
    {
        var pivots = EventMessageEntityExtractor.ExtractFromFinding(finding);

        sb.AppendLine("    <div class=\"ctf-pivots\">");
        sb.AppendLine("      <strong>CTF pivots (heuristic)</strong>");
        sb.AppendLine("      <p class=\"ctf-pivots-note\">Verify in context. Empty or generic messages limit extraction.</p>");

        if (pivots.Count == 0)
        {
            sb.AppendLine("      <p class=\"ctf-pivots-empty\">No IPv4s, paths, DOMAIN\\user-style accounts, emails, or computer names were parsed from these events.</p>");
        }
        else
        {
            sb.AppendLine("      <ul class=\"ctf-pivot-list\">");
            foreach (var p in pivots)
            {
                sb.AppendLine("        <li>");
                sb.AppendLine($"          <span class=\"pivot-cat\">{Encode(p.Category)}</span>");
                sb.AppendLine("          <span class=\"pivot-sep\"> · </span>");
                sb.AppendLine($"          <span class=\"pivot-val\">{Encode(p.Value)}</span>");
                sb.AppendLine("        </li>");
            }
            sb.AppendLine("      </ul>");
        }

        sb.AppendLine("    </div>");
    }

    private static void AppendFooter(StringBuilder sb, string generatedAt)
    {
        sb.AppendLine("<footer>");
        sb.AppendLine($"  <p>Generated by EventScanner on {generatedAt}</p>");
        sb.AppendLine("</footer>");
    }

    private static string Encode(string text) =>
        System.Net.WebUtility.HtmlEncode(text ?? "");

    private static string GetGradeColorHex(GradeLevel level) => level switch
    {
        GradeLevel.F => "#DC3545",
        GradeLevel.D => "#FF6B35",
        GradeLevel.C => "#FFA500",
        GradeLevel.B => "#9ACD32",
        GradeLevel.A => "#28A745",
        GradeLevel.S => "#00B4D8",
        GradeLevel.SS => "#8A2BE2",
        GradeLevel.SSS => "#FFD700",
        GradeLevel.SSSPlus => "#FFD700",
        _ => "#FFFFFF"
    };

    private static string GetSeverityColorHex(Severity severity) => severity switch
    {
        Severity.Critical => "#DC3545",
        Severity.High => "#FF8C00",
        Severity.Medium => "#FFC107",
        Severity.Low => "#00B4D8",
        Severity.Informational => "#6C757D",
        _ => "#FFFFFF"
    };

    private static string GetSeverityBgHex(Severity severity) => severity switch
    {
        Severity.Critical => "#DC354520",
        Severity.High => "#FF8C0020",
        Severity.Medium => "#FFC10718",
        Severity.Low => "#00B4D818",
        Severity.Informational => "#6C757D15",
        _ => "#FFFFFF10"
    };

    private const string CssStyles = """
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
            background: #0d1117; color: #c9d1d9;
            line-height: 1.6; padding: 32px; max-width: 960px; margin: 0 auto;
        }
        header { text-align: center; padding: 40px 0 32px; }
        header h1 { font-size: 18px; opacity: 0.5; font-weight: 400; letter-spacing: 2px; text-transform: uppercase; }
        .grade { font-size: 96px; font-weight: 800; margin: 16px 0 8px; }
        .score { font-size: 22px; opacity: 0.8; font-weight: 600; }
        .description { font-size: 14px; opacity: 0.5; margin-top: 8px; }
        h2 { font-size: 20px; margin-bottom: 16px; padding-bottom: 8px; border-bottom: 1px solid #21262d; }
        .summary { background: #161b22; border-radius: 12px; padding: 24px; margin-bottom: 24px; }
        .summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; }
        .stat { display: flex; flex-direction: column; }
        .stat .label { font-size: 12px; opacity: 0.5; text-transform: uppercase; letter-spacing: 1px; }
        .stat .value { font-size: 16px; font-weight: 600; margin-top: 4px; }
        .findings h2 { margin-top: 8px; }
        .finding {
            background: #161b22; border-radius: 10px; padding: 20px;
            margin-bottom: 12px; border: 1px solid #21262d;
        }
        .finding-header { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
        .severity-badge {
            padding: 3px 10px; border-radius: 4px; font-size: 12px;
            font-weight: 600; white-space: nowrap;
        }
        .finding-title { font-size: 16px; font-weight: 600; flex: 1; }
        .finding-meta { font-size: 12px; opacity: 0.5; }
        .finding-desc { font-size: 13px; opacity: 0.7; margin-top: 8px; }
        .detail-section { font-size: 13px; margin-top: 12px; opacity: 0.85; }
        .detail-section strong { opacity: 1; }
        .detail-section ul { margin-top: 4px; padding-left: 20px; }
        .detail-section li { margin-bottom: 2px; }
        details { margin-top: 12px; }
        summary { cursor: pointer; font-size: 13px; font-weight: 600; opacity: 0.8; padding: 4px 0; }
        summary:hover { opacity: 1; }
        .events-table { width: 100%; border-collapse: collapse; margin-top: 8px; font-size: 12px; }
        .events-table th {
            text-align: left; padding: 8px; background: #21262d;
            font-weight: 600; font-size: 11px; text-transform: uppercase;
            letter-spacing: 0.5px; position: sticky; top: 0;
        }
        .events-table td { padding: 6px 8px; border-top: 1px solid #21262d; vertical-align: top; }
        .events-table tr:hover td { background: #1c2128; }
        .no-findings { text-align: center; padding: 32px; opacity: 0.5; font-size: 16px; }
        .ctf-pivots {
            margin-top: 12px; padding: 12px 14px; background: #0d1117;
            border: 1px solid #30363d; border-radius: 8px; font-size: 13px;
        }
        .ctf-pivots-note { font-size: 11px; opacity: 0.55; margin: 4px 0 10px; line-height: 1.4; }
        .ctf-pivots-empty { opacity: 0.55; font-style: italic; margin: 0; }
        .ctf-pivot-list { list-style: none; padding: 0; margin: 0; }
        .ctf-pivot-list li { margin: 6px 0; }
        .pivot-cat { font-weight: 600; }
        .pivot-sep { opacity: 0.45; }
        .pivot-val { word-break: break-all; }
        footer { text-align: center; padding: 32px 0; opacity: 0.3; font-size: 12px; }
        """;
}
