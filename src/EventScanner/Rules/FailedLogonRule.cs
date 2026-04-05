using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects failed logon attempts that may indicate unauthorized access attempts.
///
/// Looks for:
///   - Event ID 4625 in the Security log (audit logon failure)
///
/// Note: The Security log often requires administrator privileges to read.
/// If the app is not running as admin, this rule will simply find no events.
///
/// This is a well-documented standard Windows security audit event.
/// </summary>
public sealed class FailedLogonRule : IRule
{
    public string RuleId => "SEC-LOGON-001";
    public string RuleName => "Failed Logon Attempts";
    public string TargetLog => "Security";

    private const int FailedLogonEventId = 4625;

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == FailedLogonEventId)
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 20 => Severity.High,
            >= 5 => Severity.Medium,
            _ => Severity.Informational
        };

        var confidence = matches.Count >= 5
            ? Confidence.High
            : Confidence.Medium;

        var oldest = matches.Min(e => e.TimeCreated);
        var newest = matches.Max(e => e.TimeCreated);

        var countText = matches.Count == 1
            ? "1 failed logon attempt"
            : $"{matches.Count} failed logon attempts";

        return
        [
            new Finding(
                title: "Failed Logon Attempts Detected",
                description: $"Windows recorded {countText} during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: confidence,
                category: "Security",
                relatedEventIds: [FailedLogonEventId],
                explanation: "Someone (or something) tried to log into your computer but "
                    + "used the wrong password or credentials. A few failed attempts are "
                    + "normal — everyone mistypes their password sometimes. A large number "
                    + "in a short period could mean someone is trying to guess your password.",
                whyItMatters: "Failed logon attempts are one of the most common indicators of "
                    + "unauthorized access attempts. While a handful are usually harmless, "
                    + "a high volume could indicate a brute-force attack, especially if "
                    + "the machine is exposed to a network.",
                possibleCauses:
                [
                    "Mistyped password during normal login",
                    "Saved credentials that are now outdated",
                    "A scheduled task or service using old credentials",
                    "Another user on the network trying to access shared resources",
                    "Automated brute-force password guessing (if count is high)"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: oldest,
                lastOccurrence: newest,
                matchedEvents: matches)
        ];
    }
}
