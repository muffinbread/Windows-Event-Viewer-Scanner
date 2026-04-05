using EventScanner.Models;
using EventScanner.Rules;

namespace EventScanner.Services;

/// <summary>
/// Runs all registered detection rules against raw event log entries.
/// Pre-filters events by log name so each rule only sees events from its target log.
/// Collects and returns all findings.
/// </summary>
public sealed class EventAnalyzer : IEventAnalyzer
{
    private readonly IReadOnlyList<IRule> _rules;

    /// <summary>
    /// Creates an analyzer with the specified detection rules.
    /// </summary>
    public EventAnalyzer(IEnumerable<IRule> rules)
    {
        _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <summary>
    /// Creates an analyzer with the default set of built-in detection rules.
    /// </summary>
    public static EventAnalyzer CreateWithDefaultRules()
    {
        return new EventAnalyzer(
        [
            // System log rules
            new UnexpectedShutdownRule(),
            new DiskErrorRule(),
            new ServiceCrashRule(),
            new NtfsErrorRule(),
            new WindowsUpdateFailureRule(),
            new BugCheckRule(),
            new TimeServiceErrorRule(),

            // Application log rules
            new ApplicationCrashRule(),
            new ApplicationHangRule(),

            // Security log rules
            new FailedLogonRule(),
            new AuditLogClearedRule(),
            new UserAccountChangeRule(),
            new AccountLockoutRule(),
            new GroupMembershipChangeRule(),
            new AuditPolicyChangeRule(),
            new ScheduledTaskRule(),

            // System log rules (additional)
            new NewServiceInstalledRule(),

            // PowerShell log rules
            new PowerShellActivityRule()
        ]);
    }

    public IReadOnlyList<Finding> Analyze(IReadOnlyList<EventLogEntry> events)
    {
        if (events == null || events.Count == 0)
            return Array.Empty<Finding>();

        var eventsByLog = events
            .GroupBy(e => e.LogName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EventLogEntry>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var allFindings = new List<Finding>();

        foreach (var rule in _rules)
        {
            if (eventsByLog.TryGetValue(rule.TargetLog, out var logEvents))
            {
                var findings = rule.Evaluate(logEvents);
                allFindings.AddRange(findings);
            }
        }

        allFindings.Sort((a, b) => b.Severity.CompareTo(a.Severity));

        return allFindings;
    }
}
