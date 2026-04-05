using EventScanner.Models;
using EventScanner.Rules;

namespace EventScanner.Tests.Rules;

/// <summary>
/// Helper to create fake event log entries for testing rules
/// without needing real Windows event logs.
/// </summary>
internal static class TestEventFactory
{
    public static EventLogEntry Create(
        int eventId,
        string logName = "System",
        string providerName = "TestProvider",
        EventLogLevel level = EventLogLevel.Error,
        DateTime? timeCreated = null) =>
        new(
            eventId: eventId,
            logName: logName,
            providerName: providerName,
            level: level,
            timeCreated: timeCreated ?? DateTime.UtcNow);
}

public class UnexpectedShutdownRuleTests
{
    private readonly UnexpectedShutdownRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-SHUTDOWN-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoShutdownEvents_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(100, providerName: "SomeProvider"),
            TestEventFactory.Create(200, providerName: "AnotherProvider"),
        };

        var findings = _rule.Evaluate(events);
        Assert.Empty(findings);
    }

    [Fact]
    public void Evaluate_SingleEvent6008_ReturnsOneFinding()
    {
        var events = new[]
        {
            TestEventFactory.Create(6008, providerName: "EventLog"),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Low, findings[0].Severity);
        Assert.Equal(1, findings[0].OccurrenceCount);
        Assert.Equal("SYS-SHUTDOWN-001", findings[0].RuleId);
    }

    [Fact]
    public void Evaluate_SingleEvent41_KernelPower_ReturnsOneFinding()
    {
        var events = new[]
        {
            TestEventFactory.Create(41, providerName: "Microsoft-Windows-Kernel-Power"),
        };

        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
    }

    [Fact]
    public void Evaluate_TwoOccurrences_SeverityIsMedium()
    {
        var events = new[]
        {
            TestEventFactory.Create(6008, providerName: "EventLog"),
            TestEventFactory.Create(41, providerName: "Microsoft-Windows-Kernel-Power"),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
        Assert.Equal(2, findings[0].OccurrenceCount);
    }

    [Fact]
    public void Evaluate_FiveOccurrences_SeverityIsHigh()
    {
        var events = Enumerable.Range(0, 5)
            .Select(_ => TestEventFactory.Create(6008, providerName: "EventLog"))
            .ToList();

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_IgnoresWrongProvider()
    {
        var events = new[]
        {
            TestEventFactory.Create(6008, providerName: "WrongProvider"),
        };

        var findings = _rule.Evaluate(events);
        Assert.Empty(findings);
    }

    [Fact]
    public void Evaluate_CapturesMatchedEvents()
    {
        var events = new[]
        {
            TestEventFactory.Create(6008, providerName: "EventLog"),
            TestEventFactory.Create(41, providerName: "Microsoft-Windows-Kernel-Power"),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(2, findings[0].MatchedEvents.Count);
    }

    [Fact]
    public void Evaluate_TracksFirstAndLastOccurrence()
    {
        var early = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
        var late = new DateTime(2026, 4, 4, 15, 0, 0, DateTimeKind.Utc);

        var events = new[]
        {
            TestEventFactory.Create(6008, providerName: "EventLog", timeCreated: early),
            TestEventFactory.Create(6008, providerName: "EventLog", timeCreated: late),
        };

        var findings = _rule.Evaluate(events);

        Assert.Equal(early, findings[0].FirstOccurrence);
        Assert.Equal(late, findings[0].LastOccurrence);
    }
}

public class DiskErrorRuleTests
{
    private readonly DiskErrorRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-DISK-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoDiskEvents_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(100, providerName: "SomeProvider"),
        };

        var findings = _rule.Evaluate(events);
        Assert.Empty(findings);
    }

    [Fact]
    public void Evaluate_SingleDiskError_SeverityIsMedium()
    {
        var events = new[]
        {
            TestEventFactory.Create(7, providerName: "disk"),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
        Assert.Equal("Disk Health", findings[0].Category);
    }

    [Fact]
    public void Evaluate_ThreeDiskErrors_SeverityIsHigh()
    {
        var events = new[]
        {
            TestEventFactory.Create(7, providerName: "disk"),
            TestEventFactory.Create(11, providerName: "disk"),
            TestEventFactory.Create(15, providerName: "disk"),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.High, findings[0].Severity);
        Assert.Equal(3, findings[0].OccurrenceCount);
    }

    [Fact]
    public void Evaluate_TenPlusDiskErrors_SeverityIsCritical()
    {
        var events = Enumerable.Range(0, 12)
            .Select(_ => TestEventFactory.Create(7, providerName: "disk"))
            .ToList();

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_CaseInsensitiveProvider()
    {
        var events = new[]
        {
            TestEventFactory.Create(7, providerName: "Disk"),
            TestEventFactory.Create(11, providerName: "DISK"),
        };

        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
        Assert.Equal(2, findings[0].OccurrenceCount);
    }

    [Fact]
    public void Evaluate_CapturesMatchedEvents_SortedByTime()
    {
        var late = new DateTime(2026, 4, 4, 15, 0, 0, DateTimeKind.Utc);
        var early = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);

        var events = new[]
        {
            TestEventFactory.Create(7, providerName: "disk", timeCreated: late),
            TestEventFactory.Create(11, providerName: "disk", timeCreated: early),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(2, findings[0].MatchedEvents.Count);
        Assert.Equal(early, findings[0].MatchedEvents[0].TimeCreated);
        Assert.Equal(late, findings[0].MatchedEvents[1].TimeCreated);
    }

    [Fact]
    public void Evaluate_AllRelevantEventIds_AreDetected()
    {
        var events = new[]
        {
            TestEventFactory.Create(7, providerName: "disk"),
            TestEventFactory.Create(11, providerName: "disk"),
            TestEventFactory.Create(15, providerName: "disk"),
            TestEventFactory.Create(51, providerName: "disk"),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(4, findings[0].OccurrenceCount);
    }
}

public class ServiceCrashRuleTests
{
    private readonly ServiceCrashRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-SERVICE-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoServiceEvents_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(100, providerName: "SomeProvider"),
        };

        var findings = _rule.Evaluate(events);
        Assert.Empty(findings);
    }

    [Fact]
    public void Evaluate_SingleServiceCrash_SeverityIsLow()
    {
        var events = new[]
        {
            TestEventFactory.Create(7031, providerName: "Service Control Manager"),
        };

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Low, findings[0].Severity);
        Assert.Equal("Services", findings[0].Category);
    }

    [Fact]
    public void Evaluate_ThreeServiceCrashes_SeverityIsMedium()
    {
        var events = Enumerable.Range(0, 3)
            .Select(_ => TestEventFactory.Create(7034, providerName: "Service Control Manager"))
            .ToList();

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_TenPlusServiceCrashes_SeverityIsHigh()
    {
        var events = Enumerable.Range(0, 11)
            .Select(_ => TestEventFactory.Create(7031, providerName: "Service Control Manager"))
            .ToList();

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.High, findings[0].Severity);
    }
}

public class FailedLogonRuleTests
{
    private readonly FailedLogonRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SEC-LOGON-001", _rule.RuleId);
        Assert.Equal("Security", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoFailedLogons_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(4624, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
        };

        var findings = _rule.Evaluate(events);
        Assert.Empty(findings);
    }

    [Fact]
    public void Evaluate_FewFailedLogons_SeverityIsInformational()
    {
        var events = Enumerable.Range(0, 3)
            .Select(_ => TestEventFactory.Create(4625, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Informational, findings[0].Severity);
        Assert.Equal(Confidence.Medium, findings[0].Confidence);
    }

    [Fact]
    public void Evaluate_FiveFailedLogons_SeverityIsMedium()
    {
        var events = Enumerable.Range(0, 5)
            .Select(_ => TestEventFactory.Create(4625, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
        Assert.Equal(Confidence.High, findings[0].Confidence);
    }

    [Fact]
    public void Evaluate_TwentyPlusFailedLogons_SeverityIsHigh()
    {
        var events = Enumerable.Range(0, 25)
            .Select(_ => TestEventFactory.Create(4625, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();

        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.High, findings[0].Severity);
    }
}
