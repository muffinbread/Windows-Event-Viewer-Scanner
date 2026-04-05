using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects applications that stopped responding (hung/froze).
///
/// Looks for:
///   - Event ID 1002 from "Application Hang" (application not responding)
/// </summary>
public sealed class ApplicationHangRule : IRule
{
    public string RuleId => "APP-HANG-001";
    public string RuleName => "Application Hangs";
    public string TargetLog => "Application";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 1002
                && e.ProviderName.Equals("Application Hang", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 15 => Severity.Medium,
            >= 5 => Severity.Low,
            _ => Severity.Informational
        };

        return
        [
            new Finding(
                title: "Application Hangs Detected",
                description: $"Windows recorded {matches.Count} application hang(s) — programs that stopped responding.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.Medium,
                category: "Application Stability",
                relatedEventIds: [1002],
                explanation: "One or more programs froze — they stopped responding to clicks "
                    + "and keyboard input. Windows may have waited for them to recover "
                    + "or forcefully closed them.",
                whyItMatters: "Frequent hangs of the same application suggest it may need to be "
                    + "updated, reinstalled, or investigated for compatibility issues. "
                    + "System-wide hangs may point to hardware or resource problems.",
                possibleCauses:
                [
                    "Application waiting for a resource that is unavailable",
                    "Insufficient RAM causing excessive disk paging",
                    "Disk I/O bottleneck (slow or failing drive)",
                    "Deadlock in the application's code (software bug)",
                    "Network timeout if the app depends on a remote server"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
