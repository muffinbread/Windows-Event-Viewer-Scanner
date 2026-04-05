using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects changes to the system audit policy — potential evidence tampering.
///
/// Looks for:
///   - Event ID 4719 from "Microsoft-Windows-Security-Auditing" (audit policy changed)
///
/// Audit policies control what Windows records in the Security log.
/// Disabling auditing is a common anti-forensics technique — if logging is
/// turned off, the attacker's subsequent actions won't be recorded.
/// </summary>
public sealed class AuditPolicyChangeRule : IRule
{
    public string RuleId => "SEC-POLICY-001";
    public string RuleName => "Audit Policy Change";
    public string TargetLog => "Security";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 4719
                && e.ProviderName.Equals("Microsoft-Windows-Security-Auditing",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 3 => Severity.Critical,
            _ => Severity.High
        };

        return
        [
            new Finding(
                title: "Audit Policy Was Changed",
                description: $"The system's audit policy was modified {matches.Count} time(s) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Security",
                relatedEventIds: [4719],
                explanation: "The audit policy controls what Windows records in the Security "
                    + "event log. Changing it can enable or disable logging of specific "
                    + "activities like logins, file access, or privilege use.",
                whyItMatters: "Changing audit policy is a common anti-forensics technique. "
                    + "An attacker may disable logging before performing malicious actions "
                    + "so those actions aren't recorded. Unless an administrator made "
                    + "this change intentionally, it should be investigated immediately.",
                possibleCauses:
                [
                    "Attacker disabling security logging to cover tracks",
                    "System administrator adjusting audit settings",
                    "Group Policy update changing audit configuration",
                    "Security software modifying audit policy during installation",
                    "Automated hardening script adjusting settings"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
