using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects user account creation and deletion — important for forensics.
///
/// Looks for:
///   - Event ID 4720 from "Microsoft-Windows-Security-Auditing" (user account created)
///   - Event ID 4726 from "Microsoft-Windows-Security-Auditing" (user account deleted)
///
/// Unexpected account creation can indicate an attacker establishing persistence.
/// Account deletion shortly after creation is a common cleanup pattern.
/// </summary>
public sealed class UserAccountChangeRule : IRule
{
    public string RuleId => "SEC-ACCOUNT-001";
    public string RuleName => "User Account Changes";
    public string TargetLog => "Security";

    private static readonly HashSet<int> AccountEventIds = [4720, 4726];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => AccountEventIds.Contains(e.EventId)
                && e.ProviderName.Equals("Microsoft-Windows-Security-Auditing",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var createdCount = matches.Count(e => e.EventId == 4720);
        var deletedCount = matches.Count(e => e.EventId == 4726);

        var severity = matches.Count switch
        {
            >= 5 => Severity.High,
            >= 2 => Severity.Medium,
            _ => Severity.Medium
        };

        var parts = new List<string>();
        if (createdCount > 0) parts.Add($"{createdCount} created");
        if (deletedCount > 0) parts.Add($"{deletedCount} deleted");
        var summary = string.Join(", ", parts);

        return
        [
            new Finding(
                title: "User Account Changes Detected",
                description: $"User accounts were modified during the scanned period: {summary}.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Security",
                relatedEventIds: AccountEventIds.ToArray(),
                explanation: "Windows recorded that user accounts were created or deleted. "
                    + "This is normal when an administrator adds or removes users, but "
                    + "unexpected account changes — especially accounts you don't recognize — "
                    + "can be a sign of unauthorized access.",
                whyItMatters: "Attackers often create new user accounts to maintain access to "
                    + "a compromised system. They may also create and then quickly delete "
                    + "accounts to run commands with elevated privileges. Check the event "
                    + "details to see which accounts were affected and who performed the action.",
                possibleCauses:
                [
                    "Administrator creating a new user account",
                    "Automated provisioning system (Active Directory, scripts)",
                    "Attacker creating a backdoor account for persistent access",
                    "Cleanup of old or unused accounts",
                    "Malware creating hidden service accounts"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
