using EventScanner.Models;
using EventScanner.Rules;
using EventScanner.Services;
using EventScanner.Tests.Rules;

namespace EventScanner.Tests.Services;

public class EventAnalyzerTests
{
    [Fact]
    public void Analyze_EmptyEvents_ReturnsEmpty()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();

        var findings = analyzer.Analyze(Array.Empty<EventLogEntry>());

        Assert.Empty(findings);
    }

    [Fact]
    public void Analyze_NullEvents_ReturnsEmpty()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();

        var findings = analyzer.Analyze(null!);

        Assert.Empty(findings);
    }

    [Fact]
    public void Analyze_EventsWithNoIssues_ReturnsEmpty()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();
        var events = new[]
        {
            TestEventFactory.Create(100, logName: "System", providerName: "SomeProvider",
                level: EventLogLevel.Informational),
            TestEventFactory.Create(200, logName: "Application", providerName: "SomeApp",
                level: EventLogLevel.Informational),
        };

        var findings = analyzer.Analyze(events);

        Assert.Empty(findings);
    }

    [Fact]
    public void Analyze_MixedEvents_FindsMultipleIssues()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();
        var events = new[]
        {
            TestEventFactory.Create(6008, logName: "System", providerName: "EventLog"),
            TestEventFactory.Create(7, logName: "System", providerName: "disk"),
            TestEventFactory.Create(100, logName: "System", providerName: "SomeProvider"),
        };

        var findings = analyzer.Analyze(events);

        Assert.Equal(2, findings.Count);
        Assert.Contains(findings, f => f.RuleId == "SYS-SHUTDOWN-001");
        Assert.Contains(findings, f => f.RuleId == "SYS-DISK-001");
    }

    [Fact]
    public void Analyze_FiltersEventsByLogForEachRule()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();

        // Event ID 6008 in Application log should NOT trigger the shutdown rule
        // because the shutdown rule targets the System log.
        var events = new[]
        {
            TestEventFactory.Create(6008, logName: "Application", providerName: "EventLog"),
        };

        var findings = analyzer.Analyze(events);

        Assert.Empty(findings);
    }

    [Fact]
    public void Analyze_SortsFindingsBySeverityDescending()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();
        var events = new List<EventLogEntry>();

        // Add 1 shutdown event (Low severity)
        events.Add(TestEventFactory.Create(6008, logName: "System", providerName: "EventLog"));

        // Add 10 disk errors (Critical severity)
        events.AddRange(Enumerable.Range(0, 10)
            .Select(_ => TestEventFactory.Create(7, logName: "System", providerName: "disk")));

        var findings = analyzer.Analyze(events);

        Assert.Equal(2, findings.Count);
        Assert.True(findings[0].Severity >= findings[1].Severity,
            "Findings should be sorted by severity, most severe first");
    }

    [Fact]
    public void Analyze_SecurityLogEvents_ProcessedBySecurityRules()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();
        var events = Enumerable.Range(0, 6)
            .Select(_ => TestEventFactory.Create(4625, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();

        var findings = analyzer.Analyze(events);

        Assert.Single(findings);
        Assert.Equal("SEC-LOGON-001", findings[0].RuleId);
    }

    [Fact]
    public void Analyze_CustomRules_AreUsed()
    {
        var customRule = new UnexpectedShutdownRule();
        var analyzer = new EventAnalyzer(new IRule[] { customRule });

        var events = new[]
        {
            TestEventFactory.Create(6008, logName: "System", providerName: "EventLog"),
        };

        var findings = analyzer.Analyze(events);

        Assert.Single(findings);
        Assert.Equal("SYS-SHUTDOWN-001", findings[0].RuleId);
    }

    [Fact]
    public void CreateWithDefaultRules_IncludesAllBuiltInRules()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();
        var events = new List<EventLogEntry>
        {
            TestEventFactory.Create(6008, logName: "System", providerName: "EventLog"),
            TestEventFactory.Create(7, logName: "System", providerName: "disk"),
            TestEventFactory.Create(7031, logName: "System", providerName: "Service Control Manager"),
            TestEventFactory.Create(4625, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"),
        };

        var findings = analyzer.Analyze(events);

        Assert.Equal(4, findings.Count);
        Assert.Contains(findings, f => f.RuleId == "SYS-SHUTDOWN-001");
        Assert.Contains(findings, f => f.RuleId == "SYS-DISK-001");
        Assert.Contains(findings, f => f.RuleId == "SYS-SERVICE-001");
        Assert.Contains(findings, f => f.RuleId == "SEC-LOGON-001");
    }
}
