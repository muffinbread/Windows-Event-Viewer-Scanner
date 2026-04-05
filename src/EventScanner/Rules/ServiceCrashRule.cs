using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects Windows services that terminated unexpectedly.
///
/// Looks for:
///   - Event ID 7031 from "Service Control Manager" (service terminated unexpectedly)
///   - Event ID 7034 from "Service Control Manager" (service terminated unexpectedly)
///
/// These are well-documented standard Windows service events.
/// </summary>
public sealed class ServiceCrashRule : IRule
{
    public string RuleId => "SYS-SERVICE-001";
    public string RuleName => "Service Crash";
    public string TargetLog => "System";

    private static readonly HashSet<int> ServiceCrashEventIds = [7031, 7034];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => ServiceCrashEventIds.Contains(e.EventId) && IsServiceControlManager(e))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 10 => Severity.High,
            >= 3 => Severity.Medium,
            _ => Severity.Low
        };

        var oldest = matches.Min(e => e.TimeCreated);
        var newest = matches.Max(e => e.TimeCreated);

        var countText = matches.Count == 1
            ? "1 service crash"
            : $"{matches.Count} service crashes";

        return
        [
            new Finding(
                title: "Windows Service Crash Detected",
                description: $"Windows reported {countText} during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.Medium,
                category: "Services",
                relatedEventIds: ServiceCrashEventIds.ToArray(),
                explanation: "One or more Windows services (background programs that handle "
                    + "things like printing, networking, or updates) stopped working "
                    + "unexpectedly. Windows may have automatically restarted them.",
                whyItMatters: "Service crashes can cause features to stop working temporarily. "
                    + "Occasional crashes are usually harmless, but frequent crashes of the "
                    + "same service may indicate a deeper problem such as corrupted files, "
                    + "incompatible software, or insufficient system resources.",
                possibleCauses:
                [
                    "Corrupted system files affecting the service",
                    "Software conflict with a recently installed program",
                    "Insufficient memory (RAM) causing the service to fail",
                    "A Windows Update that introduced a bug",
                    "Malware interfering with system services"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: oldest,
                lastOccurrence: newest,
                matchedEvents: matches)
        ];
    }

    private static bool IsServiceControlManager(EventLogEntry entry)
    {
        return entry.ProviderName.Equals("Service Control Manager", StringComparison.OrdinalIgnoreCase);
    }
}
