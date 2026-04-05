using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects NTFS file system errors indicating possible disk corruption.
///
/// Looks for:
///   - Event ID 55 from "Ntfs" (file system structure corruption detected)
///   - Event ID 98 from "Ntfs" (volume could not be mounted)
///   - Event ID 137 from "Ntfs" (transaction resource manager error)
/// </summary>
public sealed class NtfsErrorRule : IRule
{
    public string RuleId => "SYS-NTFS-001";
    public string RuleName => "NTFS File System Errors";
    public string TargetLog => "System";

    private static readonly HashSet<int> NtfsEventIds = [55, 98, 137];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => NtfsEventIds.Contains(e.EventId)
                && e.ProviderName.Equals("Ntfs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 5 => Severity.Critical,
            >= 2 => Severity.High,
            _ => Severity.High
        };

        return
        [
            new Finding(
                title: "NTFS File System Errors Detected",
                description: $"Windows reported {matches.Count} file system error(s) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Disk Health",
                relatedEventIds: NtfsEventIds.ToArray(),
                explanation: "The NTFS file system — the way Windows organizes files on your "
                    + "drive — reported structural errors. This means some files or folders "
                    + "may be damaged or inaccessible.",
                whyItMatters: "File system corruption can cause data loss, application crashes, "
                    + "and boot failures. It often indicates a failing drive, improper shutdowns, "
                    + "or serious software problems.",
                possibleCauses:
                [
                    "Hard drive or SSD hardware failure",
                    "Repeated unexpected shutdowns or power loss",
                    "Corrupted Windows system files",
                    "Malware modifying file system structures",
                    "Faulty RAM causing write errors to disk"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
