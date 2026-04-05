using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects when the security audit log was cleared — a major forensics red flag.
///
/// Looks for:
///   - Event ID 1102 from "Microsoft-Windows-Eventlog" (audit log cleared)
///
/// In incident response, this is one of the first things investigators look for.
/// Legitimate log clearing is rare; attackers clear logs to hide their activity.
/// </summary>
public sealed class AuditLogClearedRule : IRule
{
    public string RuleId => "SEC-AUDIT-001";
    public string RuleName => "Audit Log Cleared";
    public string TargetLog => "Security";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 1102
                && e.ProviderName.Equals("Microsoft-Windows-Eventlog", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = Severity.Critical;

        return
        [
            new Finding(
                title: "Security Audit Log Was Cleared",
                description: $"The security audit log was cleared {matches.Count} time(s) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Security",
                relatedEventIds: [1102],
                explanation: "Someone or something cleared the Windows Security event log. "
                    + "This erases the record of logins, permission changes, and other "
                    + "security-related activity. In forensics, this is considered highly "
                    + "suspicious because attackers often clear logs to hide what they did.",
                whyItMatters: "The security log is the primary evidence trail for who accessed "
                    + "the system and what they did. Clearing it destroys that evidence. "
                    + "Unless a system administrator intentionally cleared the log for "
                    + "maintenance, this should be investigated.",
                possibleCauses:
                [
                    "An attacker covering their tracks after a compromise",
                    "System administrator performing routine maintenance",
                    "Automated cleanup script running with excessive permissions",
                    "Malware designed to erase forensic evidence"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
