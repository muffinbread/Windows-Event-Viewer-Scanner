using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects Windows Time service errors (clock synchronization failures).
///
/// Looks for:
///   - Event ID 129 from "Microsoft-Windows-Time-Service" (NTP server unreachable)
///   - Event ID 134 from "Microsoft-Windows-Time-Service" (configuration error)
/// </summary>
public sealed class TimeServiceErrorRule : IRule
{
    public string RuleId => "SYS-TIME-001";
    public string RuleName => "Time Service Errors";
    public string TargetLog => "System";

    private static readonly HashSet<int> TimeEventIds = [129, 134];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => TimeEventIds.Contains(e.EventId)
                && (e.ProviderName.Equals("Microsoft-Windows-Time-Service", StringComparison.OrdinalIgnoreCase)
                    || e.ProviderName.Equals("W32Time", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 10 => Severity.Medium,
            >= 3 => Severity.Low,
            _ => Severity.Informational
        };

        return
        [
            new Finding(
                title: "Time Synchronization Errors",
                description: $"Windows failed to sync the system clock {matches.Count} time(s) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.Medium,
                category: "System Configuration",
                relatedEventIds: TimeEventIds.ToArray(),
                explanation: "Windows could not synchronize your computer's clock with an "
                    + "internet time server. This usually means the time server was "
                    + "unreachable or there was a network issue.",
                whyItMatters: "An inaccurate system clock can cause security certificate errors, "
                    + "authentication failures, and incorrect timestamps on files and logs. "
                    + "In forensics, accurate time is critical for correlating events.",
                possibleCauses:
                [
                    "No internet connection when sync was attempted",
                    "Firewall blocking NTP traffic (port 123)",
                    "Time server is down or misconfigured",
                    "CMOS battery dying (causes clock to reset on reboot)",
                    "Network configuration issues"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
