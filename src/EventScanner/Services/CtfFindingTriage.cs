using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Re-orders findings for fast manual review without changing detection or grading.
/// </summary>
public static class CtfFindingTriage
{
    /// <summary>
    /// Severity (highest first), then how often the pattern appeared, then most recent event time.
    /// Stable tie-breaker: title, then rule id.
    /// </summary>
    public static IReadOnlyList<Finding> OrderForSpeed(IReadOnlyList<Finding> findings)
    {
        if (findings == null || findings.Count == 0)
            return Array.Empty<Finding>();

        return findings
            .OrderByDescending(f => f.Severity)
            .ThenByDescending(f => f.OccurrenceCount)
            .ThenByDescending(f => f.LastOccurrence ?? DateTime.MinValue)
            .ThenBy(f => f.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.RuleId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
