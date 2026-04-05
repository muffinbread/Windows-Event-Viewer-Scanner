using EventScanner.Models;
using EventScanner.Rules;
using EventScanner.Services;

namespace EventScanner.Tests.Rules;

public class NtfsErrorRuleTests
{
    private readonly NtfsErrorRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-NTFS-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoNtfsEvents_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(100, providerName: "Other") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleNtfsError_ReturnsHighSeverity()
    {
        var events = new[] { TestEventFactory.Create(55, providerName: "Ntfs") };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.High, findings[0].Severity);
        Assert.Equal("Disk Health", findings[0].Category);
        Assert.Single(findings[0].MatchedEvents);
    }

    [Fact]
    public void Evaluate_FiveNtfsErrors_CriticalSeverity()
    {
        var events = Enumerable.Range(0, 5)
            .Select(_ => TestEventFactory.Create(55, providerName: "Ntfs"))
            .ToList();
        var findings = _rule.Evaluate(events);

        Assert.Equal(Severity.Critical, findings[0].Severity);
    }
}

public class WindowsUpdateFailureRuleTests
{
    private readonly WindowsUpdateFailureRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-UPDATE-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoUpdateEvents_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(100, providerName: "Other") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleUpdateFailure_LowSeverity()
    {
        var events = new[]
        {
            TestEventFactory.Create(20, providerName: "Microsoft-Windows-WindowsUpdateClient")
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Low, findings[0].Severity);
        Assert.Equal("Updates", findings[0].Category);
    }

    [Fact]
    public void Evaluate_ManyUpdateFailures_HighSeverity()
    {
        var events = Enumerable.Range(0, 12)
            .Select(_ => TestEventFactory.Create(20,
                providerName: "Microsoft-Windows-WindowsUpdateClient"))
            .ToList();
        var findings = _rule.Evaluate(events);

        Assert.Equal(Severity.High, findings[0].Severity);
    }
}

public class BugCheckRuleTests
{
    private readonly BugCheckRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-BSOD-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoBugChecks_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(100, providerName: "Other") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleBSOD_MediumSeverity()
    {
        var events = new[]
        {
            TestEventFactory.Create(1001,
                providerName: "Microsoft-Windows-WER-SystemErrorReporting")
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
        Assert.Equal("System Stability", findings[0].Category);
    }

    [Fact]
    public void Evaluate_FiveBSODs_CriticalSeverity()
    {
        var events = Enumerable.Range(0, 5)
            .Select(_ => TestEventFactory.Create(1001,
                providerName: "Microsoft-Windows-WER-SystemErrorReporting"))
            .ToList();
        var findings = _rule.Evaluate(events);

        Assert.Equal(Severity.Critical, findings[0].Severity);
    }
}

public class TimeServiceErrorRuleTests
{
    private readonly TimeServiceErrorRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-TIME-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoTimeErrors_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(100, providerName: "Other") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleTimeError_Informational()
    {
        var events = new[]
        {
            TestEventFactory.Create(129, providerName: "Microsoft-Windows-Time-Service")
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Informational, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_AcceptsW32TimeProvider()
    {
        var events = new[]
        {
            TestEventFactory.Create(134, providerName: "W32Time")
        };
        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
    }
}

public class ApplicationCrashRuleTests
{
    private readonly ApplicationCrashRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("APP-CRASH-001", _rule.RuleId);
        Assert.Equal("Application", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoCrashes_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(100, logName: "Application", providerName: "Other")
        };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleCrash_LowSeverity()
    {
        var events = new[]
        {
            TestEventFactory.Create(1000, logName: "Application",
                providerName: "Application Error")
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Low, findings[0].Severity);
        Assert.Equal("Application Stability", findings[0].Category);
    }

    [Fact]
    public void Evaluate_ManyCrashes_HighSeverity()
    {
        var events = Enumerable.Range(0, 20)
            .Select(_ => TestEventFactory.Create(1000, logName: "Application",
                providerName: "Application Error"))
            .ToList();
        var findings = _rule.Evaluate(events);

        Assert.Equal(Severity.High, findings[0].Severity);
    }
}

public class ApplicationHangRuleTests
{
    private readonly ApplicationHangRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("APP-HANG-001", _rule.RuleId);
        Assert.Equal("Application", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoHangs_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(100, logName: "Application", providerName: "Other")
        };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleHang_Informational()
    {
        var events = new[]
        {
            TestEventFactory.Create(1002, logName: "Application",
                providerName: "Application Hang")
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Informational, findings[0].Severity);
    }
}

public class AuditLogClearedRuleTests
{
    private readonly AuditLogClearedRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SEC-AUDIT-001", _rule.RuleId);
        Assert.Equal("Security", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoClearing_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(4625, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing")
        };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_AuditCleared_AlwaysCritical()
    {
        var events = new[]
        {
            TestEventFactory.Create(1102, logName: "Security",
                providerName: "Microsoft-Windows-Eventlog")
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Critical, findings[0].Severity);
        Assert.Equal("Security", findings[0].Category);
    }
}

public class UserAccountChangeRuleTests
{
    private readonly UserAccountChangeRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SEC-ACCOUNT-001", _rule.RuleId);
        Assert.Equal("Security", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoAccountChanges_ReturnsEmpty()
    {
        var events = new[]
        {
            TestEventFactory.Create(4625, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing")
        };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_AccountCreated_MediumSeverity()
    {
        var events = new[]
        {
            TestEventFactory.Create(4720, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing")
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
        Assert.Contains("1 created", findings[0].Description);
    }

    [Fact]
    public void Evaluate_AccountsCreatedAndDeleted_DescriptionShowsBoth()
    {
        var events = new[]
        {
            TestEventFactory.Create(4720, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(4726, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"),
        };
        var findings = _rule.Evaluate(events);

        Assert.Single(findings);
        Assert.Contains("1 created", findings[0].Description);
        Assert.Contains("1 deleted", findings[0].Description);
    }

    [Fact]
    public void Evaluate_ManyChanges_HighSeverity()
    {
        var events = Enumerable.Range(0, 6)
            .Select(_ => TestEventFactory.Create(4720, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();
        var findings = _rule.Evaluate(events);

        Assert.Equal(Severity.High, findings[0].Severity);
    }
}

public class AnalyzerDefaultRulesTests
{
    [Fact]
    public void CreateWithDefaultRules_Includes12Rules()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();
        var events = new List<EventLogEntry>
        {
            // System log triggers
            TestEventFactory.Create(6008, logName: "System", providerName: "EventLog"),
            TestEventFactory.Create(7, logName: "System", providerName: "disk"),
            TestEventFactory.Create(7031, logName: "System", providerName: "Service Control Manager"),
            TestEventFactory.Create(55, logName: "System", providerName: "Ntfs"),
            TestEventFactory.Create(20, logName: "System", providerName: "Microsoft-Windows-WindowsUpdateClient"),
            TestEventFactory.Create(1001, logName: "System", providerName: "Microsoft-Windows-WER-SystemErrorReporting"),
            TestEventFactory.Create(129, logName: "System", providerName: "Microsoft-Windows-Time-Service"),

            // Application log triggers
            TestEventFactory.Create(1000, logName: "Application", providerName: "Application Error"),
            TestEventFactory.Create(1002, logName: "Application", providerName: "Application Hang"),

            // Security log triggers
            TestEventFactory.Create(4625, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(1102, logName: "Security", providerName: "Microsoft-Windows-Eventlog"),
            TestEventFactory.Create(4720, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
        };

        var findings = analyzer.Analyze(events);

        Assert.Equal(12, findings.Count);
    }
}
