using EventScanner.Models;

namespace EventScanner.Tests.Models;

public class ScanResultTests
{
    private static SystemGrade CreateGrade(int score = 80) => new(score);

    private static Finding CreateFinding(Severity severity = Severity.Medium) =>
        new(
            title: "Test Finding",
            description: "Test",
            severity: severity,
            sourceLog: "System",
            ruleId: "TEST-001");

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var start = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 4, 10, 5, 0, DateTimeKind.Utc);
        var rangeStart = new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc);
        var grade = CreateGrade(85);
        var findings = new[] { CreateFinding() };
        var logs = new[] { "System", "Application" };

        var result = new ScanResult(
            scanType: ScanType.Quick,
            startedAt: start,
            completedAt: end,
            scanRangeStart: rangeStart,
            scanRangeEnd: end,
            scannedLogs: logs,
            findings: findings,
            grade: grade,
            totalEventsAnalyzed: 500);

        Assert.Equal(ScanType.Quick, result.ScanType);
        Assert.Equal(start, result.StartedAt);
        Assert.Equal(end, result.CompletedAt);
        Assert.Equal(rangeStart, result.ScanRangeStart);
        Assert.Equal(logs, result.ScannedLogs);
        Assert.Single(result.Findings);
        Assert.Equal(85, result.Grade.Score);
        Assert.Equal(500, result.TotalEventsAnalyzed);
        Assert.Equal(ScanAnalysisProfile.Standard, result.AnalysisProfile);
    }

    [Fact]
    public void Constructor_WithCtfProfile_SetsAnalysisProfile()
    {
        var start = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddMinutes(1);

        var result = new ScanResult(
            scanType: ScanType.Import,
            startedAt: start,
            completedAt: end,
            scanRangeStart: start,
            scanRangeEnd: end,
            scannedLogs: ["Security"],
            findings: Array.Empty<Finding>(),
            grade: CreateGrade(100),
            totalEventsAnalyzed: 0,
            analysisProfile: ScanAnalysisProfile.CtfSpeed);

        Assert.Equal(ScanAnalysisProfile.CtfSpeed, result.AnalysisProfile);
    }

    [Fact]
    public void Duration_CalculatedFromStartAndEnd()
    {
        var start = new DateTime(2026, 4, 4, 10, 0, 0);
        var end = new DateTime(2026, 4, 4, 10, 2, 30);

        var result = new ScanResult(
            scanType: ScanType.Quick,
            startedAt: start,
            completedAt: end,
            scanRangeStart: start.AddDays(-1),
            scanRangeEnd: end,
            scannedLogs: new[] { "System" },
            findings: Array.Empty<Finding>(),
            grade: CreateGrade(100));

        Assert.Equal(TimeSpan.FromMinutes(2.5), result.Duration);
    }

    [Fact]
    public void Constructor_WithNoFindings_IsValid()
    {
        var result = new ScanResult(
            scanType: ScanType.Deep,
            startedAt: DateTime.UtcNow,
            completedAt: DateTime.UtcNow,
            scanRangeStart: DateTime.UtcNow.AddDays(-30),
            scanRangeEnd: DateTime.UtcNow,
            scannedLogs: new[] { "System", "Security", "Application" },
            findings: Array.Empty<Finding>(),
            grade: CreateGrade(100));

        Assert.Empty(result.Findings);
        Assert.Equal(GradeLevel.SSSPlus, result.Grade.Grade);
    }

    [Fact]
    public void Constructor_NullGrade_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ScanResult(
                scanType: ScanType.Quick,
                startedAt: DateTime.UtcNow,
                completedAt: DateTime.UtcNow,
                scanRangeStart: DateTime.UtcNow.AddDays(-1),
                scanRangeEnd: DateTime.UtcNow,
                scannedLogs: new[] { "System" },
                findings: Array.Empty<Finding>(),
                grade: null!));
    }

    [Fact]
    public void Constructor_NullFindings_DefaultsToEmpty()
    {
        var result = new ScanResult(
            scanType: ScanType.Quick,
            startedAt: DateTime.UtcNow,
            completedAt: DateTime.UtcNow,
            scanRangeStart: DateTime.UtcNow.AddDays(-1),
            scanRangeEnd: DateTime.UtcNow,
            scannedLogs: new[] { "System" },
            findings: null!,
            grade: CreateGrade());

        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Constructor_NegativeEventsAnalyzed_ClampedToZero()
    {
        var result = new ScanResult(
            scanType: ScanType.Quick,
            startedAt: DateTime.UtcNow,
            completedAt: DateTime.UtcNow,
            scanRangeStart: DateTime.UtcNow.AddDays(-1),
            scanRangeEnd: DateTime.UtcNow,
            scannedLogs: new[] { "System" },
            findings: Array.Empty<Finding>(),
            grade: CreateGrade(),
            totalEventsAnalyzed: -10);

        Assert.Equal(0, result.TotalEventsAnalyzed);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        var result1 = new ScanResult(
            ScanType.Quick, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            new[] { "System" }, Array.Empty<Finding>(), CreateGrade());

        var result2 = new ScanResult(
            ScanType.Quick, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            new[] { "System" }, Array.Empty<Finding>(), CreateGrade());

        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public void Constructor_MultipleFindings_AllPreserved()
    {
        var findings = new[]
        {
            CreateFinding(Severity.Critical),
            CreateFinding(Severity.High),
            CreateFinding(Severity.Low),
        };

        var result = new ScanResult(
            ScanType.Deep, DateTime.UtcNow, DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow,
            new[] { "System" }, findings, CreateGrade(50));

        Assert.Equal(3, result.Findings.Count);
    }
}
