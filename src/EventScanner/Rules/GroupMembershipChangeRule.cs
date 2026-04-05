using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects when users are added to security groups — a privilege escalation indicator.
///
/// Looks for:
///   - Event ID 4728 (member added to security-enabled global group)
///   - Event ID 4732 (member added to security-enabled local group)
///   - Event ID 4756 (member added to security-enabled universal group)
///
/// Attackers who gain access to a system often add themselves or a backdoor
/// account to the Administrators group to escalate privileges.
/// </summary>
public sealed class GroupMembershipChangeRule : IRule
{
    public string RuleId => "SEC-GROUP-001";
    public string RuleName => "Group Membership Changes";
    public string TargetLog => "Security";

    private static readonly HashSet<int> GroupChangeEventIds = [4728, 4732, 4756];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => GroupChangeEventIds.Contains(e.EventId)
                && e.ProviderName.Equals("Microsoft-Windows-Security-Auditing",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 5 => Severity.High,
            >= 2 => Severity.Medium,
            _ => Severity.Medium
        };

        return
        [
            new Finding(
                title: "Security Group Membership Changes",
                description: $"{matches.Count} user(s) were added to security groups during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Security",
                relatedEventIds: GroupChangeEventIds.ToArray(),
                explanation: "One or more users were added to Windows security groups. Security "
                    + "groups control what permissions users have — being added to the "
                    + "Administrators group, for example, gives full control of the system.",
                whyItMatters: "Adding users to privileged groups is a common privilege escalation "
                    + "technique. If you don't recognize the change, someone may have "
                    + "gained unauthorized administrator access. Check the event details "
                    + "to see which account was added and to which group.",
                possibleCauses:
                [
                    "Administrator granting access to a new user",
                    "Attacker escalating privileges after gaining initial access",
                    "Automated provisioning system (Active Directory)",
                    "Software installation requiring admin group membership",
                    "Backdoor account being added to Administrators group"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
