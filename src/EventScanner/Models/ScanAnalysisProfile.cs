namespace EventScanner.Models;

/// <summary>
/// How scan results are prepared for the analyst. Does not change which rules run
/// or how grading works — only presentation ordering and labels.
/// </summary>
public enum ScanAnalysisProfile
{
    /// <summary>
    /// Default ordering (usually by severity as produced by the analyzer).
    /// </summary>
    Standard,

    /// <summary>
    /// Optimized for fast manual triage (e.g. CTFs): same detections and grades,
    /// findings re-sorted for quick pivoting.
    /// </summary>
    CtfSpeed
}
