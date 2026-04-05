using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects unexpected system shutdowns (crashes, power loss, forced shutdowns).
///
/// Looks for:
///   - Event ID 6008 from "EventLog" provider (unexpected shutdown recorded on next boot)
///   - Event ID 41 from "Microsoft-Windows-Kernel-Power" (kernel power failure)
///
/// These are well-documented standard Windows events.
/// </summary>
public sealed class UnexpectedShutdownRule : IRule
{
    public string RuleId => "SYS-SHUTDOWN-001";
    public string RuleName => "Unexpected System Shutdown";
    public string TargetLog => "System";

    private static readonly HashSet<int> ShutdownEventIds = [6008, 41];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => ShutdownEventIds.Contains(e.EventId) && IsRelevantProvider(e))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 5 => Severity.High,
            >= 2 => Severity.Medium,
            _ => Severity.Low
        };

        var oldest = matches.Min(e => e.TimeCreated);
        var newest = matches.Max(e => e.TimeCreated);

        var countText = matches.Count == 1
            ? "once"
            : $"{matches.Count} times";

        return
        [
            new Finding(
                title: "Unexpected System Shutdown Detected",
                description: $"Your system shut down unexpectedly {countText} during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "System Stability",
                relatedEventIds: ShutdownEventIds.ToArray(),
                explanation: "Windows recorded that your computer shut down without going "
                    + "through the normal shutdown process. This means the system either "
                    + "crashed, lost power, or was forcefully turned off.",
                whyItMatters: "Unexpected shutdowns can cause data loss, file corruption, "
                    + "and may indicate hardware problems, driver issues, or power supply "
                    + "instability. Repeated occurrences are a stronger warning sign.",
                possibleCauses:
                [
                    "Power outage or loose power cable",
                    "Overheating causing automatic shutdown",
                    "Blue screen crash (BSOD) from a driver or hardware fault",
                    "Holding the power button to force shutdown",
                    "Failing power supply unit (PSU)"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: oldest,
                lastOccurrence: newest,
                matchedEvents: matches)
        ];
    }

    private static bool IsRelevantProvider(EventLogEntry entry)
    {
        return entry.ProviderName.Equals("EventLog", StringComparison.OrdinalIgnoreCase)
            || entry.ProviderName.Equals("Microsoft-Windows-Kernel-Power", StringComparison.OrdinalIgnoreCase);
    }
}
