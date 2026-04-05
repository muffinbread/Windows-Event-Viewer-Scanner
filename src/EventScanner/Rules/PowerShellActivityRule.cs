using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects PowerShell engine activity — critical for forensics investigations.
///
/// Looks for:
///   - Event ID 400 from "PowerShell" (engine started / state changed to Available)
///
/// PowerShell is the most commonly used tool in modern cyberattacks.
/// While PowerShell usage is normal for administrators and scripts,
/// unexpected PowerShell activity — especially at odd hours or on
/// machines where it shouldn't be used — is a key forensic indicator.
/// </summary>
public sealed class PowerShellActivityRule : IRule
{
    public string RuleId => "PS-EXEC-001";
    public string RuleName => "PowerShell Activity";
    public string TargetLog => "Windows PowerShell";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 400
                && e.ProviderName.Equals("PowerShell", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 50 => Severity.Medium,
            >= 10 => Severity.Low,
            _ => Severity.Informational
        };

        return
        [
            new Finding(
                title: "PowerShell Activity Detected",
                description: $"PowerShell was launched {matches.Count} time(s) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.Low,
                category: "Forensics",
                relatedEventIds: [400],
                explanation: "PowerShell — a powerful command-line tool built into Windows — "
                    + "was started one or more times. PowerShell can manage system settings, "
                    + "run scripts, download files, and perform administrative tasks.",
                whyItMatters: "PowerShell is the single most commonly abused tool in modern "
                    + "cyberattacks. Attackers use it to download malware, move laterally "
                    + "across networks, steal credentials, and exfiltrate data. While "
                    + "PowerShell usage is normal for IT tasks, unexpected activity — "
                    + "especially outside business hours or on end-user machines — "
                    + "warrants investigation. Check the timeline for patterns.",
                possibleCauses:
                [
                    "System administrator running management scripts",
                    "Software installation or update using PowerShell",
                    "Automated maintenance tasks (Group Policy, SCCM)",
                    "Attacker executing commands after gaining access",
                    "Malware using PowerShell for fileless execution"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
