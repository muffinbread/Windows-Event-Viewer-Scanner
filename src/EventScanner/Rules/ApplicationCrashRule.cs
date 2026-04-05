using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects application crashes recorded by Windows Error Reporting.
///
/// Looks for:
///   - Event ID 1000 from "Application Error" (faulting application)
/// </summary>
public sealed class ApplicationCrashRule : IRule
{
    public string RuleId => "APP-CRASH-001";
    public string RuleName => "Application Crashes";
    public string TargetLog => "Application";

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => e.EventId == 1000
                && e.ProviderName.Equals("Application Error", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 15 => Severity.High,
            >= 5 => Severity.Medium,
            _ => Severity.Low
        };

        return
        [
            new Finding(
                title: "Application Crashes Detected",
                description: $"Windows recorded {matches.Count} application crash(es) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Application Stability",
                relatedEventIds: [1000],
                explanation: "One or more programs crashed — they stopped working unexpectedly. "
                    + "The event message usually includes the name of the crashing program "
                    + "and the faulting module (the specific part that failed).",
                whyItMatters: "Occasional application crashes are normal, but frequent crashes "
                    + "of the same program may indicate corrupted installation files, "
                    + "compatibility issues, or malware interference.",
                possibleCauses:
                [
                    "Corrupted application installation",
                    "Outdated or incompatible software version",
                    "Insufficient system resources (RAM, disk space)",
                    "Conflict with another installed program",
                    "Malware interfering with the application"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
