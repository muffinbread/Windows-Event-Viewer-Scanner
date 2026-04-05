namespace EventScanner.Models;

/// <summary>
/// Represents a single issue or observation detected by analyzing event logs.
/// Each Finding is one "line item" on the scan report.
/// </summary>
public sealed class Finding
{
    /// <summary>
    /// Unique identifier for this finding. Auto-generated.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Short name of the issue (e.g., "Repeated Disk Errors").
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Detailed description of what was detected.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// How severe this issue is (Critical, High, Medium, Low, Informational).
    /// </summary>
    public Severity Severity { get; }

    /// <summary>
    /// How confident the analysis is that this is a real issue.
    /// </summary>
    public Confidence Confidence { get; }

    /// <summary>
    /// What area this finding belongs to (e.g., "Disk Health", "Security", "Services").
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Which Windows Event Log this finding came from (e.g., "System", "Security", "Application").
    /// </summary>
    public string SourceLog { get; }

    /// <summary>
    /// The Windows Event IDs that triggered this finding.
    /// For example, Event ID 7 is a disk error in the System log.
    /// </summary>
    public IReadOnlyList<int> RelatedEventIds { get; }

    /// <summary>
    /// Plain-language explanation of what this finding means.
    /// Written for someone who is not a system administrator.
    /// </summary>
    public string Explanation { get; }

    /// <summary>
    /// Why the user should care about this finding.
    /// </summary>
    public string WhyItMatters { get; }

    /// <summary>
    /// Possible reasons this issue might be happening.
    /// </summary>
    public IReadOnlyList<string> PossibleCauses { get; }

    /// <summary>
    /// How many times this pattern appeared in the scanned logs.
    /// </summary>
    public int OccurrenceCount { get; }

    /// <summary>
    /// When the earliest related event occurred.
    /// Null if the time could not be determined.
    /// </summary>
    public DateTime? FirstOccurrence { get; }

    /// <summary>
    /// When the most recent related event occurred.
    /// Null if the time could not be determined.
    /// </summary>
    public DateTime? LastOccurrence { get; }

    /// <summary>
    /// When this finding was created by the analyzer.
    /// </summary>
    public DateTime DetectedAt { get; }

    /// <summary>
    /// Whether the user has acknowledged/reviewed this finding.
    /// Mutable because the user can change this after the scan.
    /// </summary>
    public bool IsReviewed { get; set; }

    /// <summary>
    /// Whether the user has chosen to suppress similar findings in the future.
    /// Mutable because the user can change this after the scan.
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// The rule identifier that produced this finding.
    /// Used to link findings back to detection rules for suppression and deduplication.
    /// </summary>
    public string RuleId { get; }

    /// <summary>
    /// The actual raw event log entries that triggered this finding.
    /// Sorted by time (oldest first) to form a forensic timeline.
    /// Used in the detail view to show each individual event.
    /// </summary>
    public IReadOnlyList<EventLogEntry> MatchedEvents { get; }

    public Finding(
        string title,
        string description,
        Severity severity,
        string sourceLog,
        string ruleId,
        Confidence confidence = Confidence.High,
        string category = "General",
        IReadOnlyList<int>? relatedEventIds = null,
        string explanation = "",
        string whyItMatters = "",
        IReadOnlyList<string>? possibleCauses = null,
        int occurrenceCount = 1,
        DateTime? firstOccurrence = null,
        DateTime? lastOccurrence = null,
        IReadOnlyList<EventLogEntry>? matchedEvents = null,
        DateTime? detectedAt = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Finding must have a title.", nameof(title));

        if (string.IsNullOrWhiteSpace(sourceLog))
            throw new ArgumentException("Finding must specify a source log.", nameof(sourceLog));

        if (string.IsNullOrWhiteSpace(ruleId))
            throw new ArgumentException("Finding must specify a rule ID.", nameof(ruleId));

        Id = Guid.NewGuid().ToString("N");
        Title = title.Trim();
        Description = description?.Trim() ?? "";
        Severity = severity;
        Confidence = confidence;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        SourceLog = sourceLog.Trim();
        RuleId = ruleId.Trim();
        RelatedEventIds = relatedEventIds ?? Array.Empty<int>();
        Explanation = explanation?.Trim() ?? "";
        WhyItMatters = whyItMatters?.Trim() ?? "";
        PossibleCauses = possibleCauses ?? Array.Empty<string>();
        OccurrenceCount = Math.Max(occurrenceCount, 1);
        FirstOccurrence = firstOccurrence;
        LastOccurrence = lastOccurrence;
        DetectedAt = detectedAt ?? DateTime.UtcNow;

        var sortedEvents = matchedEvents?.ToList() ?? [];
        sortedEvents.Sort((a, b) =>
            (a.TimeCreated ?? DateTime.MinValue).CompareTo(b.TimeCreated ?? DateTime.MinValue));
        MatchedEvents = sortedEvents;
        IsReviewed = false;
        IsSuppressed = false;
    }
}
