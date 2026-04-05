using EventScanner.Models;

namespace EventScanner.Rules;

/// <summary>
/// Contract for a detection rule. Each rule knows how to look for
/// one specific type of issue in event log entries.
///
/// The analyzer pre-filters events by TargetLog before calling Evaluate,
/// so rules only receive events from the log they care about.
/// </summary>
public interface IRule
{
    /// <summary>
    /// Unique identifier for this rule (e.g., "SYS-SHUTDOWN-001").
    /// Used to link findings back to rules for suppression and deduplication.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Human-readable name for this rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Which event log this rule analyzes (e.g., "System", "Security", "Application").
    /// </summary>
    string TargetLog { get; }

    /// <summary>
    /// Analyzes a set of event log entries and returns zero or more findings.
    /// Returns an empty list if nothing noteworthy is found — that's normal and good.
    /// </summary>
    /// <param name="events">
    /// Events from this rule's TargetLog only (pre-filtered by the analyzer).
    /// </param>
    IReadOnlyList<Finding> Evaluate(IReadOnlyList<EventLogEntry> events);
}
