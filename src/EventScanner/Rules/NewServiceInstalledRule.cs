using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects new Windows service installations — a persistence and execution mechanism.
///
/// Looks for:
///   - Event ID 7045 from "Service Control Manager" (a service was installed)
///
/// Malware often installs itself as a Windows service to run at startup
/// with system-level privileges. Legitimate service installations also generate
/// this event, so context matters — check the service name and executable path.
/// </summary>
public sealed class NewServiceInstalledRule : IRule
{
    public string RuleId => "SYS-NEWSVC-001";
    public string RuleName => "New Service Installed";
    public string TargetLog => "System";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 7045
                && e.ProviderName.Equals("Service Control Manager",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 10 => Severity.High,
            >= 3 => Severity.Medium,
            _ => Severity.Low
        };

        return
        [
            new Finding(
                title: "New Service(s) Installed",
                description: $"{matches.Count} new Windows service(s) were installed during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.Medium,
                category: "Security",
                relatedEventIds: [7045],
                explanation: "A new Windows service was registered. Services are programs that "
                    + "run in the background, often starting automatically when the computer "
                    + "boots. The event message contains the service name and the path to "
                    + "the executable file.",
                whyItMatters: "While many legitimate programs install services (antivirus, "
                    + "drivers, cloud sync), attackers also use services to maintain "
                    + "persistent access. A service running as SYSTEM has the highest "
                    + "privileges. Check the executable path for anything suspicious.",
                possibleCauses:
                [
                    "Legitimate software installation (driver, tool, update)",
                    "Attacker installing a backdoor service for persistence",
                    "Remote administration tool being deployed",
                    "System management software being configured",
                    "Malware disguising itself as a legitimate service name"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
