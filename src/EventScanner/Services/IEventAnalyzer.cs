using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Contract for the event analysis engine.
/// Takes raw event log entries, runs all detection rules,
/// and returns the findings.
/// </summary>
public interface IEventAnalyzer
{
    /// <summary>
    /// Analyzes raw event log entries using all registered detection rules.
    /// Each rule looks for a specific type of issue.
    /// Returns all findings discovered across all rules.
    /// </summary>
    IReadOnlyList<Finding> Analyze(IReadOnlyList<EventLogEntry> events);
}
