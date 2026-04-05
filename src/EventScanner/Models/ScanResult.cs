namespace EventScanner.Models;

/// <summary>
/// The complete output of a scan — all findings, the computed grade,
/// and metadata about the scan itself.
/// </summary>
public sealed class ScanResult
{
    /// <summary>
    /// Unique identifier for this scan.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Whether this was a Quick scan (24h) or Deep scan (30 days).
    /// </summary>
    public ScanType ScanType { get; }

    /// <summary>
    /// When the scan started running.
    /// </summary>
    public DateTime StartedAt { get; }

    /// <summary>
    /// When the scan finished.
    /// </summary>
    public DateTime CompletedAt { get; }

    /// <summary>
    /// How long the scan took to run.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// The earliest point in time the scan looked at.
    /// For Quick scans, this is ~24 hours before the scan.
    /// For Deep scans, this is ~30 days before the scan.
    /// </summary>
    public DateTime ScanRangeStart { get; }

    /// <summary>
    /// The latest point in time the scan looked at (usually when the scan ran).
    /// </summary>
    public DateTime ScanRangeEnd { get; }

    /// <summary>
    /// Which event logs were scanned (e.g., "System", "Security", "Application").
    /// </summary>
    public IReadOnlyList<string> ScannedLogs { get; }

    /// <summary>
    /// All findings detected during this scan.
    /// </summary>
    public IReadOnlyList<Finding> Findings { get; }

    /// <summary>
    /// The overall system grade computed from the findings.
    /// </summary>
    public SystemGrade Grade { get; }

    /// <summary>
    /// How findings were prepared for review (standard ordering vs CTF triage ordering).
    /// Does not change which rules ran or the grade — only sort order and labels.
    /// </summary>
    public ScanAnalysisProfile AnalysisProfile { get; }

    /// <summary>
    /// Total number of raw event log entries that were analyzed.
    /// </summary>
    public int TotalEventsAnalyzed { get; }

    public ScanResult(
        ScanType scanType,
        DateTime startedAt,
        DateTime completedAt,
        DateTime scanRangeStart,
        DateTime scanRangeEnd,
        IReadOnlyList<string> scannedLogs,
        IReadOnlyList<Finding> findings,
        SystemGrade grade,
        int totalEventsAnalyzed = 0,
        ScanAnalysisProfile analysisProfile = ScanAnalysisProfile.Standard)
    {
        Id = Guid.NewGuid().ToString("N");
        ScanType = scanType;
        AnalysisProfile = analysisProfile;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        ScanRangeStart = scanRangeStart;
        ScanRangeEnd = scanRangeEnd;
        ScannedLogs = scannedLogs ?? Array.Empty<string>();
        Findings = findings ?? Array.Empty<Finding>();
        Grade = grade ?? throw new ArgumentNullException(nameof(grade));
        TotalEventsAnalyzed = Math.Max(totalEventsAnalyzed, 0);
    }
}
