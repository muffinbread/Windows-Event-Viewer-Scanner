namespace EventScanner.Models;

/// <summary>
/// The severity level assigned by Windows to an event log entry.
/// These map directly to the standard Windows event levels.
/// Not to be confused with our Severity enum, which is for findings.
/// </summary>
public enum EventLogLevel
{
    /// <summary>Level 0 — Always logged regardless of filter settings.</summary>
    LogAlways = 0,

    /// <summary>Level 1 — A severe error that caused a major failure.</summary>
    Critical = 1,

    /// <summary>Level 2 — A significant problem such as a loss of functionality.</summary>
    Error = 2,

    /// <summary>Level 3 — A condition that is not immediately harmful but may cause problems.</summary>
    Warning = 3,

    /// <summary>Level 4 — Normal operational information.</summary>
    Informational = 4,

    /// <summary>Level 5 — Detailed diagnostic/debugging information.</summary>
    Verbose = 5
}
