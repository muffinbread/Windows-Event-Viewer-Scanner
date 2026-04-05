namespace EventScanner.Models;

/// <summary>
/// System health/security grade tiers, from worst (F) to best (SSSPlus).
/// The integer values allow direct comparison: higher is better.
/// </summary>
public enum GradeLevel
{
    F = 0,
    D = 1,
    C = 2,
    B = 3,
    A = 4,
    S = 5,
    SS = 6,
    SSS = 7,
    SSSPlus = 8
}

/// <summary>
/// Helper methods for working with GradeLevel values.
/// Extension methods attach new behavior to an existing type without modifying it.
/// </summary>
public static class GradeLevelExtensions
{
    /// <summary>
    /// Returns the human-readable display name for a grade level.
    /// Needed because C# enum names can't contain characters like "+".
    /// </summary>
    public static string ToDisplayName(this GradeLevel grade) => grade switch
    {
        GradeLevel.F => "F",
        GradeLevel.D => "D",
        GradeLevel.C => "C",
        GradeLevel.B => "B",
        GradeLevel.A => "A",
        GradeLevel.S => "S",
        GradeLevel.SS => "SS",
        GradeLevel.SSS => "SSS",
        GradeLevel.SSSPlus => "SSS+",
        _ => "Unknown"
    };

    /// <summary>
    /// Returns a short description of what this grade means.
    /// </summary>
    public static string ToDescription(this GradeLevel grade) => grade switch
    {
        GradeLevel.F => "Serious issues found — immediate attention recommended",
        GradeLevel.D => "Many problems detected — action needed",
        GradeLevel.C => "Below average — notable issues present",
        GradeLevel.B => "Decent health — some issues to address",
        GradeLevel.A => "Good system health — well maintained",
        GradeLevel.S => "Excellent — your system is in great shape",
        GradeLevel.SS => "Outstanding — very clean and well-configured",
        GradeLevel.SSS => "Near-perfect — almost nothing to improve",
        GradeLevel.SSSPlus => "Virtually flawless — top-tier system health",
        _ => "Unknown grade"
    };

    /// <summary>
    /// Converts a numeric score (0–100) to the corresponding grade level.
    /// Scores are clamped to 0–100 to handle out-of-range input safely.
    /// </summary>
    public static GradeLevel FromScore(int score)
    {
        var clamped = Math.Clamp(score, 0, 100);

        return clamped switch
        {
            >= 99 => GradeLevel.SSSPlus,
            >= 94 => GradeLevel.SSS,
            >= 88 => GradeLevel.SS,
            >= 80 => GradeLevel.S,
            >= 65 => GradeLevel.A,
            >= 50 => GradeLevel.B,
            >= 35 => GradeLevel.C,
            >= 20 => GradeLevel.D,
            _ => GradeLevel.F
        };
    }
}
