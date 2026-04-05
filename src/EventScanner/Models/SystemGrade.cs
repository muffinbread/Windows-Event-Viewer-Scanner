namespace EventScanner.Models;

/// <summary>
/// The overall system health/security grade produced by a scan.
/// This is the "report card" — the main output the user sees.
/// </summary>
public sealed class SystemGrade
{
    /// <summary>
    /// Numeric score from 0 (worst) to 100 (best).
    /// This is computed by the grading engine based on all findings.
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// The grade tier derived from the score (F through SSS+).
    /// </summary>
    public GradeLevel Grade { get; }

    /// <summary>
    /// Human-readable grade name (e.g., "SSS+", "A", "F").
    /// </summary>
    public string DisplayName => Grade.ToDisplayName();

    /// <summary>
    /// Short description of what this grade means.
    /// </summary>
    public string Description => Grade.ToDescription();

    /// <summary>
    /// When this grade was computed.
    /// </summary>
    public DateTime GradedAt { get; }

    /// <summary>
    /// Total number of findings across all severity levels.
    /// </summary>
    public int TotalFindings => CriticalCount + HighCount + MediumCount + LowCount + InformationalCount;

    /// <summary>Number of Critical severity findings.</summary>
    public int CriticalCount { get; }

    /// <summary>Number of High severity findings.</summary>
    public int HighCount { get; }

    /// <summary>Number of Medium severity findings.</summary>
    public int MediumCount { get; }

    /// <summary>Number of Low severity findings.</summary>
    public int LowCount { get; }

    /// <summary>Number of Informational severity findings.</summary>
    public int InformationalCount { get; }

    /// <summary>
    /// Creates a new SystemGrade from a score and finding severity counts.
    /// The grade level is automatically derived from the score.
    /// </summary>
    public SystemGrade(
        int score,
        int criticalCount = 0,
        int highCount = 0,
        int mediumCount = 0,
        int lowCount = 0,
        int informationalCount = 0,
        DateTime? gradedAt = null)
    {
        Score = Math.Clamp(score, 0, 100);
        Grade = GradeLevelExtensions.FromScore(Score);
        CriticalCount = Math.Max(criticalCount, 0);
        HighCount = Math.Max(highCount, 0);
        MediumCount = Math.Max(mediumCount, 0);
        LowCount = Math.Max(lowCount, 0);
        InformationalCount = Math.Max(informationalCount, 0);
        GradedAt = gradedAt ?? DateTime.UtcNow;
    }
}
