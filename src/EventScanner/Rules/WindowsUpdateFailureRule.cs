using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Detects failed Windows Update installations.
///
/// Looks for:
///   - Event ID 20 from "Microsoft-Windows-WindowsUpdateClient" (installation failure)
///   - Event ID 25 from "Microsoft-Windows-WindowsUpdateClient" (uninstall failure)
/// </summary>
public sealed class WindowsUpdateFailureRule : IRule
{
    public string RuleId => "SYS-UPDATE-001";
    public string RuleName => "Windows Update Failures";
    public string TargetLog => "System";

    private static readonly HashSet<int> UpdateEventIds = [20, 25];

    public IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events)
    {
        var matches = events
            .Where(e => UpdateEventIds.Contains(e.EventId)
                && e.ProviderName.StartsWith("Microsoft-Windows-WindowsUpdateClient",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return Array.Empty<Finding>();

        var severity = matches.Count switch
        {
            >= 10 => Severity.High,
            >= 3 => Severity.Medium,
            _ => Severity.Low
        };

        return
        [
            new Finding(
                title: "Windows Update Failures Detected",
                description: $"Windows failed to install {matches.Count} update(s) during the scanned period.",
                severity: severity,
                sourceLog: TargetLog,
                ruleId: RuleId,
                confidence: Confidence.High,
                category: "Updates",
                relatedEventIds: UpdateEventIds.ToArray(),
                explanation: "One or more Windows Updates failed to install. Updates contain "
                    + "security patches and bug fixes. When updates fail, your system may be "
                    + "missing important protections.",
                whyItMatters: "Failed updates can leave your system vulnerable to known security "
                    + "exploits. Attackers often target unpatched systems because the "
                    + "vulnerabilities are publicly documented.",
                possibleCauses:
                [
                    "Insufficient disk space for the update",
                    "Network interruption during download",
                    "Conflicting software blocking the update",
                    "Corrupted Windows Update components",
                    "Third-party antivirus interfering with installation"
                ],
                occurrenceCount: matches.Count,
                firstOccurrence: matches.Min(e => e.TimeCreated),
                lastOccurrence: matches.Max(e => e.TimeCreated),
                matchedEvents: matches)
        ];
    }
}
