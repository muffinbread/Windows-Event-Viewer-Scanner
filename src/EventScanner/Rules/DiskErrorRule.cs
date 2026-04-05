using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects disk read/write errors that may indicate a failing hard drive.
///
/// Looks for:
///   - Event ID 7 from "disk" provider (bad block detected)
///   - Event ID 11 from "disk" provider (controller error)
///   - Event ID 15 from "disk" provider (device not ready / timeout)
///   - Event ID 51 from "disk" provider (paging error during I/O)
///
/// These are well-documented standard Windows disk events.
/// </summary>
public sealed class DiskErrorRule : IRule
{
    public string RuleId => "SYS-DISK-001";
    public string RuleName => "Disk Errors";
    public string TargetLog => "System";

    private static readonly HashSet<int> DiskEventIds = [7, 11, 15, 51];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => DiskEventIds.Contains(e.EventId) && IsDiskProvider(e))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 10 => Severity.Critical,
            >= 3 => Severity.High,
            _ => Severity.Medium
        };

        var oldest = matches.Min(e => e.TimeCreated);
        var newest = matches.Max(e => e.TimeCreated);

        var countText = matches.Count == 1
            ? "1 disk error"
            : $"{matches.Count} disk errors";

        return
        [
            new Finding(
                title: "Disk Errors Detected",
                description: $"Windows reported {countText} during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Disk Health",
                relatedEventIds: DiskEventIds.ToArray(),
                explanation: "Your hard drive or SSD reported errors while trying to read "
                    + "or write data. A small number of errors can sometimes be temporary, "
                    + "but repeated errors often mean the drive is starting to fail.",
                whyItMatters: "Disk errors are one of the earliest warning signs of a dying "
                    + "hard drive. If the drive fails completely, you could lose files, "
                    + "documents, photos, and programs. Early detection gives you time to "
                    + "back up your data and replace the drive.",
                possibleCauses:
                [
                    "Hard drive or SSD beginning to fail",
                    "Loose or damaged SATA/power cable",
                    "Bad sectors on the disk surface",
                    "Overheating causing temporary read/write failures",
                    "Outdated or faulty storage driver"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: oldest,
                lastOccurrence: newest,
                matchedEvents: matches)
        ];
    }

    private static bool IsDiskProvider(EventLogEntry entry)
    {
        return entry.ProviderName.Equals("disk", StringComparison.OrdinalIgnoreCase);
    }
}
