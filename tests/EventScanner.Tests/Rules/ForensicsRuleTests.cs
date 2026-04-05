using EventScanner.Models;
using EventScanner.Rules;
using EventScanner.Services;

namespace EventScanner.Tests.Rules;

public class AccountLockoutRuleTests
{
    private readonly AccountLockoutRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SEC-LOCKOUT-001", _rule.RuleId);
        Assert.Equal("Security", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoLockouts_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(4625, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleLockout_MediumSeverity()
    {
        var events = new[] { TestEventFactory.Create(4740, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_TenLockouts_CriticalSeverity()
    {
        var events = Enumerable.Range(0, 10)
            .Select(_ => TestEventFactory.Create(4740, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();
        Assert.Equal(Severity.Critical, _rule.Evaluate(events)[0].Severity);
    }
}

public class GroupMembershipChangeRuleTests
{
    private readonly GroupMembershipChangeRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SEC-GROUP-001", _rule.RuleId);
        Assert.Equal("Security", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoChanges_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(4625, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_MemberAddedToGlobalGroup_Detected()
    {
        var events = new[] { TestEventFactory.Create(4728, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
        Assert.Equal(Severity.Medium, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_MemberAddedToLocalGroup_Detected()
    {
        var events = new[] { TestEventFactory.Create(4732, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        Assert.Single(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_ManyChanges_HighSeverity()
    {
        var events = Enumerable.Range(0, 6)
            .Select(_ => TestEventFactory.Create(4756, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();
        Assert.Equal(Severity.High, _rule.Evaluate(events)[0].Severity);
    }
}

public class AuditPolicyChangeRuleTests
{
    private readonly AuditPolicyChangeRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SEC-POLICY-001", _rule.RuleId);
        Assert.Equal("Security", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoPolicyChanges_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(4625, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SinglePolicyChange_HighSeverity()
    {
        var events = new[] { TestEventFactory.Create(4719, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
        Assert.Equal(Severity.High, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_MultiplePolicyChanges_CriticalSeverity()
    {
        var events = Enumerable.Range(0, 3)
            .Select(_ => TestEventFactory.Create(4719, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"))
            .ToList();
        Assert.Equal(Severity.Critical, _rule.Evaluate(events)[0].Severity);
    }
}

public class ScheduledTaskRuleTests
{
    private readonly ScheduledTaskRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SEC-TASK-001", _rule.RuleId);
        Assert.Equal("Security", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoTaskEvents_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(4625, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_TaskCreated_Detected()
    {
        var events = new[] { TestEventFactory.Create(4698, logName: "Security",
            providerName: "Microsoft-Windows-Security-Auditing") };
        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
        Assert.Contains("1 created", findings[0].Description);
    }

    [Fact]
    public void Evaluate_TaskCreatedAndDeleted_ShowsBoth()
    {
        var events = new[]
        {
            TestEventFactory.Create(4698, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(4699, logName: "Security",
                providerName: "Microsoft-Windows-Security-Auditing"),
        };
        var findings = _rule.Evaluate(events);
        Assert.Contains("1 created", findings[0].Description);
        Assert.Contains("1 deleted", findings[0].Description);
    }
}

public class NewServiceInstalledRuleTests
{
    private readonly NewServiceInstalledRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("SYS-NEWSVC-001", _rule.RuleId);
        Assert.Equal("System", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoNewServices_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(100, providerName: "Other") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_SingleService_LowSeverity()
    {
        var events = new[] { TestEventFactory.Create(7045,
            providerName: "Service Control Manager") };
        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
        Assert.Equal(Severity.Low, findings[0].Severity);
    }

    [Fact]
    public void Evaluate_ManyServices_HighSeverity()
    {
        var events = Enumerable.Range(0, 12)
            .Select(_ => TestEventFactory.Create(7045,
                providerName: "Service Control Manager"))
            .ToList();
        Assert.Equal(Severity.High, _rule.Evaluate(events)[0].Severity);
    }
}

public class PowerShellActivityRuleTests
{
    private readonly PowerShellActivityRule _rule = new();

    [Fact]
    public void RuleId_And_TargetLog_AreCorrect()
    {
        Assert.Equal("PS-EXEC-001", _rule.RuleId);
        Assert.Equal("Windows PowerShell", _rule.TargetLog);
    }

    [Fact]
    public void Evaluate_NoPowerShell_ReturnsEmpty()
    {
        var events = new[] { TestEventFactory.Create(100,
            logName: "Windows PowerShell", providerName: "Other") };
        Assert.Empty(_rule.Evaluate(events));
    }

    [Fact]
    public void Evaluate_FewLaunches_Informational()
    {
        var events = Enumerable.Range(0, 3)
            .Select(_ => TestEventFactory.Create(400,
                logName: "Windows PowerShell", providerName: "PowerShell"))
            .ToList();
        var findings = _rule.Evaluate(events);
        Assert.Single(findings);
        Assert.Equal(Severity.Informational, findings[0].Severity);
        Assert.Equal("Forensics", findings[0].Category);
    }

    [Fact]
    public void Evaluate_ManyLaunches_MediumSeverity()
    {
        var events = Enumerable.Range(0, 55)
            .Select(_ => TestEventFactory.Create(400,
                logName: "Windows PowerShell", providerName: "PowerShell"))
            .ToList();
        Assert.Equal(Severity.Medium, _rule.Evaluate(events)[0].Severity);
    }
}

public class AnalyzerWith18RulesTests
{
    [Fact]
    public void CreateWithDefaultRules_Includes18Rules()
    {
        var analyzer = EventAnalyzer.CreateWithDefaultRules();
        var events = new List<EventLogEntry>
        {
            // System log (8 rules)
            TestEventFactory.Create(6008, logName: "System", providerName: "EventLog"),
            TestEventFactory.Create(7, logName: "System", providerName: "disk"),
            TestEventFactory.Create(7031, logName: "System", providerName: "Service Control Manager"),
            TestEventFactory.Create(55, logName: "System", providerName: "Ntfs"),
            TestEventFactory.Create(20, logName: "System", providerName: "Microsoft-Windows-WindowsUpdateClient"),
            TestEventFactory.Create(1001, logName: "System", providerName: "Microsoft-Windows-WER-SystemErrorReporting"),
            TestEventFactory.Create(129, logName: "System", providerName: "Microsoft-Windows-Time-Service"),
            TestEventFactory.Create(7045, logName: "System", providerName: "Service Control Manager"),

            // Application log (2 rules)
            TestEventFactory.Create(1000, logName: "Application", providerName: "Application Error"),
            TestEventFactory.Create(1002, logName: "Application", providerName: "Application Hang"),

            // Security log (7 rules)
            TestEventFactory.Create(4625, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(1102, logName: "Security", providerName: "Microsoft-Windows-Eventlog"),
            TestEventFactory.Create(4720, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(4740, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(4728, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(4719, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),
            TestEventFactory.Create(4698, logName: "Security", providerName: "Microsoft-Windows-Security-Auditing"),

            // PowerShell log (1 rule)
            TestEventFactory.Create(400, logName: "Windows PowerShell", providerName: "PowerShell"),
        };

        var findings = analyzer.Analyze(events);

        Assert.Equal(18, findings.Count);
    }
}
