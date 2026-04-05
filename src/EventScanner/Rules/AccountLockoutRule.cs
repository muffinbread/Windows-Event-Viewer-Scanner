using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects account lockouts — a strong indicator of brute force password attacks.
///
/// Looks for:
///   - Event ID 4740 from "Microsoft-Windows-Security-Auditing" (account locked out)
///
/// When an account gets locked out, it means too many failed password attempts
/// occurred in a short period. This is one of the clearest signs of a brute force attack.
/// </summary>
public sealed class AccountLockoutRule : IRule
{
    public string RuleId => "SEC-LOCKOUT-001";
    public string RuleName => "Account Lockout";
    public string TargetLog => "Security";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 4740
                && e.ProviderName.Equals("Microsoft-Windows-Security-Auditing",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 10 => Severity.Critical,
            >= 3 => Severity.High,
            _ => Severity.Medium
        };

        return
        [
            new Finding(
                title: "Account Lockouts Detected",
                description: $"{matches.Count} account lockout(s) occurred during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Security",
                relatedEventIds: [4740],
                explanation: "One or more user accounts were locked out because too many "
                    + "incorrect password attempts were made. Windows locks accounts "
                    + "automatically as a security measure to prevent password guessing.",
                whyItMatters: "Account lockouts are one of the strongest indicators of a "
                    + "brute force password attack. While a single lockout may be a user "
                    + "who forgot their password, multiple lockouts — especially across "
                    + "different accounts — strongly suggest an active attack.",
                possibleCauses:
                [
                    "Brute force password attack from the network",
                    "User who repeatedly forgot their password",
                    "Saved credentials in an application that expired",
                    "Automated script or service with outdated credentials",
                    "Credential stuffing attack using leaked password lists"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
