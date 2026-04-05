using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects scheduled task creation and deletion — a common persistence mechanism.
///
/// Looks for:
///   - Event ID 4698 from "Microsoft-Windows-Security-Auditing" (scheduled task created)
///   - Event ID 4699 from "Microsoft-Windows-Security-Auditing" (scheduled task deleted)
///
/// Attackers frequently create scheduled tasks to maintain access — the task runs
/// their malware automatically even after a reboot. Quick creation followed by
/// deletion is especially suspicious (run-once attack payload).
/// </summary>
public sealed class ScheduledTaskRule : IRule
{
    public string RuleId => "SEC-TASK-001";
    public string RuleName => "Scheduled Task Changes";
    public string TargetLog => "Security";

    private static readonly HashSet<int> TaskEventIds = [4698, 4699];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => TaskEventIds.Contains(e.EventId)
                && e.ProviderName.Equals("Microsoft-Windows-Security-Auditing",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var createdCount = matches.Count(e => e.EventId == 4698);
        var deletedCount = matches.Count(e => e.EventId == 4699);

        var severity = matches.Count switch
        {
            >= 5 => Severity.High,
            >= 2 => Severity.Medium,
            _ => Severity.Low
        };

        var parts = new List<string>();
        if (createdCount > 0) parts.Add($"{createdCount} created");
        if (deletedCount > 0) parts.Add($"{deletedCount} deleted");

        return
        [
            new Finding(
                title: "Scheduled Task Changes Detected",
                description: $"Scheduled tasks were modified: {string.Join(", ", parts)}.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.Medium,
                category: "Security",
                relatedEventIds: TaskEventIds.ToArray(),
                explanation: "Scheduled tasks are programs that Windows runs automatically "
                    + "on a timer or at startup. Creating or deleting them is sometimes "
                    + "normal (software updates, maintenance), but unexpected task changes "
                    + "can indicate malware establishing a foothold.",
                whyItMatters: "Scheduled tasks are one of the most common ways attackers "
                    + "maintain access to a system. A malicious task can re-launch malware "
                    + "after a reboot, download additional payloads, or exfiltrate data "
                    + "on a schedule. Check event details for the task name and action.",
                possibleCauses:
                [
                    "Software installation creating maintenance tasks",
                    "Windows Update scheduling restart tasks",
                    "Attacker creating a persistence mechanism",
                    "Administrator automating a system task",
                    "Malware creating a run-once task then deleting evidence"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
