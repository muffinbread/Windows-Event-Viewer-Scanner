namespace EventScanner.Models;

/// <summary>
/// Defines the scope of a scan.
/// Quick scans are faster but look at less history.
/// Deep scans are slower but more thorough.
/// </summary>
public enum ScanType
{
    /// <summary>
    /// Scans the last 24 hours of event logs.
    /// Faster, good for a quick health check.
    /// </summary>
    Quick,

    /// <summary>
    /// Scans the last 30 days of event logs.
    /// Slower, but catches patterns and recurring issues.
    /// </summary>
    Deep,

    /// <summary>
    /// Analyzes imported .evtx files from any machine.
    /// Reads all events in the file regardless of time range.
    /// Used for forensics and remote machine analysis.
    /// </summary>
    Import
}
