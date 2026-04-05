using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.Tests.Services;

public class ReportExporterTests
{
    private static ScanResult CreateSampleResult(int findingCount = 2, int score = 85)
    {
        var findings = new List<Finding>();
        for (int i = 0; i < findingCount; i++)
        {
            findings.Add(new Finding(
                title: $"Test Finding {i + 1}",
                description: $"Description for finding {i + 1}",
                severity: i == 0 ? Severity.High : Severity.Medium,
                sourceLog: "System",
                ruleId: $"TEST-{i + 1:D3}",
                explanation: "This is an explanation.",
                whyItMatters: "This is why it matters.",
                possibleCauses: ["Cause A", "Cause B"],
                matchedEvents:
                [
                    new EventLogEntry(7, "System", "disk", EventLogLevel.Error,
                        DateTime.UtcNow, "Test message")
                ]));
        }

        var grade = new SystemGrade(score,
            highCount: findings.Count(f => f.Severity == Severity.High),
            mediumCount: findings.Count(f => f.Severity == Severity.Medium));

        return new ScanResult(
            ScanType.Quick,
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            ["System", "Application"],
            findings,
            grade,
            totalEventsAnalyzed: 500);
    }

    [Fact]
    public void GenerateHtml_ReturnsValidHtml()
    {
        var result = CreateSampleResult();

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("<title>EventScanner Report</title>", html);
    }

    [Fact]
    public void GenerateHtml_ContainsGradeDisplay()
    {
        var result = CreateSampleResult(score: 85);

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("S", html);
        Assert.Contains("85", html);
        Assert.Contains("100", html);
    }

    [Fact]
    public void GenerateHtml_ContainsScanSummary()
    {
        var result = CreateSampleResult();

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("Quick Scan", html);
        Assert.Contains("500", html);
        Assert.Contains("System", html);
    }

    [Fact]
    public void GenerateHtml_ContainsFindings()
    {
        var result = CreateSampleResult(findingCount: 3);

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("Test Finding 1", html);
        Assert.Contains("Test Finding 2", html);
        Assert.Contains("Test Finding 3", html);
    }

    [Fact]
    public void GenerateHtml_ContainsSeverityBadges()
    {
        var result = CreateSampleResult();

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("High", html);
        Assert.Contains("Medium", html);
    }

    [Fact]
    public void GenerateHtml_ContainsExplanation()
    {
        var result = CreateSampleResult();

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("This is an explanation", html);
        Assert.Contains("This is why it matters", html);
    }

    [Fact]
    public void GenerateHtml_ContainsPossibleCauses()
    {
        var result = CreateSampleResult();

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("Cause A", html);
        Assert.Contains("Cause B", html);
    }

    [Fact]
    public void GenerateHtml_ContainsEventTimeline()
    {
        var result = CreateSampleResult();

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("Event Timeline", html);
        Assert.Contains("Test message", html);
    }

    [Fact]
    public void GenerateHtml_NoFindings_ShowsCleanMessage()
    {
        var grade = new SystemGrade(100);
        var result = new ScanResult(
            ScanType.Quick, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            ["System"], Array.Empty<Finding>(), grade);

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("No issues found", html);
    }

    [Fact]
    public void GenerateHtml_ContainsInlineCss()
    {
        var result = CreateSampleResult();

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("<style>", html);
        Assert.Contains("font-family", html);
    }

    [Fact]
    public void GenerateHtml_CtfImportProfile_MentionsTriageInSummary()
    {
        var grade = new SystemGrade(100);
        var result = new ScanResult(
            ScanType.Import, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            ["Security"], Array.Empty<Finding>(), grade,
            analysisProfile: ScanAnalysisProfile.CtfSpeed);

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("CTF triage", html);
        Assert.Contains("Imported log files", html);
    }

    [Fact]
    public void GenerateHtml_CtfProfile_IncludesPivotListWhenEventsHaveExtractableContent()
    {
        var finding = new Finding(
            title: "Failed logon",
            description: "Multiple failures",
            severity: Severity.High,
            sourceLog: "Security",
            ruleId: "RULE-4625",
            matchedEvents:
            [
                new EventLogEntry(4625, "Security", "Microsoft-Windows-Security-Auditing",
                    EventLogLevel.Informational, DateTime.UtcNow,
                    "Source network address: 10.20.30.40")
            ]);

        var grade = new SystemGrade(70, highCount: 1);
        var result = new ScanResult(
            ScanType.Import, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            ["Security"], [finding], grade,
            totalEventsAnalyzed: 1,
            analysisProfile: ScanAnalysisProfile.CtfSpeed);

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("ctf-pivots", html);
        Assert.Contains("CTF pivots (heuristic)", html);
        Assert.Contains("pivot-cat", html);
        Assert.Contains("IPv4", html);
        Assert.Contains("10.20.30.40", html);
    }

    [Fact]
    public void GenerateHtml_CtfProfile_ShowsEmptyPivotNoteWhenNothingParsed()
    {
        var finding = new Finding(
            title: "Noise",
            description: "x",
            severity: Severity.Low,
            sourceLog: "System",
            ruleId: "RULE-X",
            matchedEvents:
            [
                new EventLogEntry(1, "System", "x", EventLogLevel.Error, DateTime.UtcNow, message: "")
            ]);

        var grade = new SystemGrade(95);
        var result = new ScanResult(
            ScanType.Import, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow, DateTime.UtcNow,
            ["System"], [finding], grade,
            analysisProfile: ScanAnalysisProfile.CtfSpeed);

        var html = ReportExporter.GenerateHtml(result);

        Assert.Contains("ctf-pivots-empty", html);
        Assert.Contains("No IPv4s", html);
    }

    [Fact]
    public void GenerateHtml_StandardImport_DoesNotIncludeCtfPivotSection()
    {
        var finding = new Finding(
            title: "Issue",
            description: "d",
            severity: Severity.Medium,
            sourceLog: "System",
            ruleId: "R1",
            matchedEvents:
            [
                new EventLogEntry(1, "System", "p", EventLogLevel.Error, DateTime.UtcNow, "10.0.0.1")
            ]);

        var grade = new SystemGrade(80);
        var result = new ScanResult(
            ScanType.Import, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow, DateTime.UtcNow,
            ["System"], [finding], grade,
            analysisProfile: ScanAnalysisProfile.Standard);

        var html = ReportExporter.GenerateHtml(result);

        Assert.DoesNotContain("CTF pivots (heuristic)", html);
    }

    [Fact]
    public void GenerateHtml_HtmlEncodesUserContent()
    {
        var finding = new Finding(
            title: "Test <script>alert('xss')</script>",
            description: "Desc with & special chars",
            severity: Severity.Low,
            sourceLog: "System",
            ruleId: "TEST-001");

        var grade = new SystemGrade(90);
        var result = new ScanResult(
            ScanType.Quick, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            ["System"], [finding], grade);

        var html = ReportExporter.GenerateHtml(result);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
