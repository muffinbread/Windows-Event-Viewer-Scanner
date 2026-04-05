using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Computes an overall system grade using a point-deduction model.
/// Starts at 100 (perfect) and subtracts points for each finding
/// based on its severity and confidence.
/// </summary>
public sealed class GradingEngine : IGradingEngine
{
    private const int PerfectScore = 100;

    private static readonly Dictionary<Severity, double> SeverityDeductions = new()
    {
        [Severity.Critical] = 30,
        [Severity.High] = 15,
        [Severity.Medium] = 8,
        [Severity.Low] = 3,
        [Severity.Informational] = 1
    };

    private static readonly Dictionary<Confidence, double> ConfidenceMultipliers = new()
    {
        [Confidence.High] = 1.0,
        [Confidence.Medium] = 0.7,
        [Confidence.Low] = 0.4
    };

    public SystemGrade ComputeGrade(IReadOnlyList<Finding> findings)
    {
        if (findings == null || findings.Count == 0)
            return new SystemGrade(PerfectScore);

        var activeFindings = findings.Where(f => !f.IsSuppressed).ToList();

        if (activeFindings.Count == 0)
            return new SystemGrade(PerfectScore);

        double totalDeduction = 0;

        foreach (var finding in activeFindings)
        {
            var baseDeduction = SeverityDeductions.GetValueOrDefault(finding.Severity, 1);
            var multiplier = ConfidenceMultipliers.GetValueOrDefault(finding.Confidence, 1.0);
            totalDeduction += baseDeduction * multiplier;
        }

        var score = (int)Math.Round(PerfectScore - totalDeduction);
        score = Math.Clamp(score, 0, PerfectScore);

        var criticalCount = activeFindings.Count(f => f.Severity == Severity.Critical);
        var highCount = activeFindings.Count(f => f.Severity == Severity.High);
        var mediumCount = activeFindings.Count(f => f.Severity == Severity.Medium);
        var lowCount = activeFindings.Count(f => f.Severity == Severity.Low);
        var informationalCount = activeFindings.Count(f => f.Severity == Severity.Informational);

        return new SystemGrade(
            score: score,
            criticalCount: criticalCount,
            highCount: highCount,
            mediumCount: mediumCount,
            lowCount: lowCount,
            informationalCount: informationalCount);
    }
}
