namespace EventScanner.Models;

/// <summary>
/// Severity level for an individual finding.
/// Higher integer value = more severe.
/// </summary>
public enum Severity
{
    Informational = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Confidence level indicating how certain the analysis is about a finding.
/// </summary>
public enum Confidence
{
    Low = 0,
    Medium = 1,
    High = 2
}
