using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Contract for the grading engine that computes an overall system
/// health/security grade from a set of findings.
/// </summary>
public interface IGradingEngine
{
    /// <summary>
    /// Computes an overall system grade from the given findings.
    /// Suppressed findings are excluded from the score.
    /// </summary>
    SystemGrade ComputeGrade(IReadOnlyList<Finding> findings);
}
