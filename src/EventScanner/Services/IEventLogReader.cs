using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Contract for reading Windows Event Viewer logs.
/// The real implementation reads from actual Windows APIs.
/// A fake implementation can be used for testing without real logs.
/// </summary>
public interface IEventLogReader
{
    /// <summary>
    /// Reads event log entries from the specified log within the given time range.
    /// Runs on a background thread so the UI doesn't freeze.
    /// </summary>
    /// <param name="logName">The log to read (e.g., "System", "Security", "Application").</param>
    /// <param name="from">The earliest time to include.</param>
    /// <param name="to">The latest time to include.</param>
    /// <param name="maxEvents">Maximum number of events to return (safety limit).</param>
    /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
    /// <returns>A list of event log entries, or an empty list if the log is inaccessible.</returns>
    Task<IReadOnlyList<EventLogEntry>> ReadEventsAsync(
        string logName,
        DateTime from,
        DateTime to,
        int maxEvents = 10_000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all event log entries from an exported .evtx file.
    /// Used for forensics — analyzing logs from another machine.
    /// </summary>
    /// <param name="filePath">Full path to the .evtx file.</param>
    /// <param name="maxEvents">Maximum number of events to return (safety limit).</param>
    /// <param name="cancellationToken">Allows the operation to be cancelled.</param>
    /// <returns>A list of event log entries, or an empty list if the file is invalid.</returns>
    Task<IReadOnlyList<EventLogEntry>> ReadEventsFromFileAsync(
        string filePath,
        int maxEvents = 50_000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the names of event logs that can be read on this machine.
    /// Logs that require admin access may be excluded if the app is not
    /// running with elevated privileges.
    /// </summary>
    IReadOnlyList<string> GetAccessibleLogNames();
}
