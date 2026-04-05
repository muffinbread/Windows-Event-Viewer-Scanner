using EventScanner.Models;

namespace EventScanner.Tests.Models;

public class FindingTests
{
    private static Finding CreateValidFinding(
        string title = "Test Finding",
        string description = "A test finding",
        Severity severity = Severity.Medium,
        string sourceLog = "System",
        string ruleId = "TEST-001")
    {
        return new Finding(
            title: title,
            description: description,
            severity: severity,
            sourceLog: sourceLog,
            ruleId: ruleId);
    }

    [Fact]
    public void Constructor_ValidInput_SetsAllProperties()
    {
        var finding = new Finding(
            title: "Repeated Disk Errors",
            description: "Multiple disk read failures detected",
            severity: Severity.High,
            sourceLog: "System",
            ruleId: "SYS-DISK-001",
            confidence: Confidence.High,
            category: "Disk Health",
            relatedEventIds: new[] { 7, 11 },
            explanation: "Your hard drive reported errors while trying to read data.",
            whyItMatters: "Repeated disk errors can lead to data loss.",
            possibleCauses: new[] { "Failing hard drive", "Loose cable" },
            occurrenceCount: 15,
            firstOccurrence: new DateTime(2026, 4, 1),
            lastOccurrence: new DateTime(2026, 4, 4));

        Assert.Equal("Repeated Disk Errors", finding.Title);
        Assert.Equal("Multiple disk read failures detected", finding.Description);
        Assert.Equal(Severity.High, finding.Severity);
        Assert.Equal(Confidence.High, finding.Confidence);
        Assert.Equal("Disk Health", finding.Category);
        Assert.Equal("System", finding.SourceLog);
        Assert.Equal("SYS-DISK-001", finding.RuleId);
        Assert.Equal(new[] { 7, 11 }, finding.RelatedEventIds);
        Assert.Equal(15, finding.OccurrenceCount);
        Assert.False(finding.IsReviewed);
        Assert.False(finding.IsSuppressed);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        var finding1 = CreateValidFinding();
        var finding2 = CreateValidFinding();

        Assert.NotEqual(finding1.Id, finding2.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyTitle_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateValidFinding(title: title!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptySourceLog_Throws(string? sourceLog)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateValidFinding(sourceLog: sourceLog!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyRuleId_Throws(string? ruleId)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateValidFinding(ruleId: ruleId!));
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var finding = new Finding(
            title: "  Padded Title  ",
            description: "  Padded description  ",
            severity: Severity.Low,
            sourceLog: "  System  ",
            ruleId: "  TEST-001  ");

        Assert.Equal("Padded Title", finding.Title);
        Assert.Equal("Padded description", finding.Description);
        Assert.Equal("System", finding.SourceLog);
        Assert.Equal("TEST-001", finding.RuleId);
    }

    [Fact]
    public void Constructor_DefaultsOccurrenceCountToOne()
    {
        var finding = CreateValidFinding();
        Assert.Equal(1, finding.OccurrenceCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_ZeroOrNegativeOccurrenceCount_BecomeOne(int count)
    {
        var finding = new Finding(
            title: "Test",
            description: "Test",
            severity: Severity.Low,
            sourceLog: "System",
            ruleId: "TEST-001",
            occurrenceCount: count);

        Assert.Equal(1, finding.OccurrenceCount);
    }

    [Fact]
    public void Constructor_DefaultCategoryIsGeneral()
    {
        var finding = CreateValidFinding();
        Assert.Equal("General", finding.Category);
    }

    [Fact]
    public void Constructor_NullDescription_BecomesEmptyString()
    {
        var finding = new Finding(
            title: "Test",
            description: null!,
            severity: Severity.Low,
            sourceLog: "System",
            ruleId: "TEST-001");

        Assert.Equal("", finding.Description);
    }

    [Fact]
    public void Constructor_NoRelatedEventIds_DefaultsToEmpty()
    {
        var finding = CreateValidFinding();
        Assert.Empty(finding.RelatedEventIds);
    }

    [Fact]
    public void Constructor_NoPossibleCauses_DefaultsToEmpty()
    {
        var finding = CreateValidFinding();
        Assert.Empty(finding.PossibleCauses);
    }

    [Fact]
    public void IsReviewed_CanBeChanged()
    {
        var finding = CreateValidFinding();
        Assert.False(finding.IsReviewed);

        finding.IsReviewed = true;
        Assert.True(finding.IsReviewed);
    }

    [Fact]
    public void IsSuppressed_CanBeChanged()
    {
        var finding = CreateValidFinding();
        Assert.False(finding.IsSuppressed);

        finding.IsSuppressed = true;
        Assert.True(finding.IsSuppressed);
    }

    [Fact]
    public void Constructor_SetsDetectedAtToNow_WhenNotProvided()
    {
        var before = DateTime.UtcNow;
        var finding = CreateValidFinding();
        var after = DateTime.UtcNow;

        Assert.InRange(finding.DetectedAt, before, after);
    }

    [Fact]
    public void Constructor_NoMatchedEvents_DefaultsToEmpty()
    {
        var finding = CreateValidFinding();
        Assert.Empty(finding.MatchedEvents);
    }

    [Fact]
    public void Constructor_MatchedEvents_AreStored()
    {
        var events = new[]
        {
            new EventLogEntry(7, "System", "disk", EventLogLevel.Error, DateTime.UtcNow),
            new EventLogEntry(11, "System", "disk", EventLogLevel.Error, DateTime.UtcNow),
        };

        var finding = new Finding(
            title: "Test",
            description: "Test",
            severity: Severity.Medium,
            sourceLog: "System",
            ruleId: "TEST-001",
            matchedEvents: events);

        Assert.Equal(2, finding.MatchedEvents.Count);
    }

    [Fact]
    public void Constructor_MatchedEvents_AreSortedByTimeOldestFirst()
    {
        var late = new DateTime(2026, 4, 4, 15, 0, 0, DateTimeKind.Utc);
        var early = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
        var middle = new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc);

        var events = new[]
        {
            new EventLogEntry(7, "System", "disk", EventLogLevel.Error, late),
            new EventLogEntry(7, "System", "disk", EventLogLevel.Error, early),
            new EventLogEntry(7, "System", "disk", EventLogLevel.Error, middle),
        };

        var finding = new Finding(
            title: "Test",
            description: "Test",
            severity: Severity.Medium,
            sourceLog: "System",
            ruleId: "TEST-001",
            matchedEvents: events);

        Assert.Equal(early, finding.MatchedEvents[0].TimeCreated);
        Assert.Equal(middle, finding.MatchedEvents[1].TimeCreated);
        Assert.Equal(late, finding.MatchedEvents[2].TimeCreated);
    }
}
