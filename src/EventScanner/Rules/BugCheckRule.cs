using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects blue screen crashes (BSOD / BugCheck) recorded by Windows Error Reporting.
///
/// Looks for:
///   - Event ID 1001 from "Microsoft-Windows-WER-SystemErrorReporting" (system error report)
/// </summary>
public sealed class BugCheckRule : IRule
{
    public string RuleId => "SYS-BSOD-001";
    public string RuleName => "Blue Screen Crashes";
    public string TargetLog => "System";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 1001
                && e.ProviderName.Equals("Microsoft-Windows-WER-SystemErrorReporting",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 5 => Severity.Critical,
            >= 2 => Severity.High,
            _ => Severity.Medium
        };

        return
        [
            new Finding(
                title: "Blue Screen Crash (BSOD) Detected",
                description: $"Windows recorded {matches.Count} blue screen crash(es) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "System Stability",
                relatedEventIds: [1001],
                explanation: "Your computer experienced a \"Blue Screen of Death\" (BSOD) — "
                    + "a critical system crash where Windows had to stop everything and restart. "
                    + "The crash details are recorded in the event message.",
                whyItMatters: "Blue screen crashes indicate a serious problem — usually a hardware "
                    + "failure, driver bug, or critical system error. They can cause data loss "
                    + "and indicate the system may be unstable.",
                possibleCauses:
                [
                    "Faulty or incompatible hardware driver",
                    "Failing RAM (memory) modules",
                    "Overheating CPU or GPU",
                    "Corrupted system files",
                    "Hardware failure (motherboard, GPU, etc.)"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
