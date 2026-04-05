namespace EventScanner.Models;

/// <summary>
/// A simplified representation of a single raw Windows Event Viewer entry.
/// This is the raw data read from Windows before any analysis.
/// Not to be confused with Finding, which is produced after analysis.
/// </summary>
public sealed class EventLogEntry
{
    /// <summary>
    /// The unique record identifier assigned by Windows.
    /// Null if the event system did not assign one.
    /// </summary>
    public long? RecordId { get; }

    /// <summary>
    /// The Windows Event ID number (e.g., 7 = disk error, 6008 = unexpected shutdown).
    /// This is the key identifier used by detection rules to classify events.
    /// </summary>
    public int EventId { get; }

    /// <summary>
    /// Which log this entry came from (e.g., "System", "Security", "Application").
    /// </summary>
    public string LogName { get; }

    /// <summary>
    /// The software component that generated this event (e.g., "disk", "Ntfs", "ESENT").
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// The severity level Windows assigned to this event.
    /// </summary>
    public EventLogLevel Level { get; }

    /// <summary>
    /// When this event was created. Null if the timestamp is unavailable.
    /// </summary>
    public DateTime? TimeCreated { get; }

    /// <summary>
    /// The human-readable message describing the event.
    /// May be empty if the event source is not registered on this machine.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The machine name where this event occurred.
    /// </summary>
    public string MachineName { get; }

    /// <summary>
    /// Raw keywords flags from the event. Used by some detection rules
    /// (e.g., Security log uses keywords to indicate audit success vs failure).
    /// </summary>
    public long? Keywords { get; }

    public EventLogEntry(
        int eventId,
        string logName,
        string providerName,
        EventLogLevel level,
        DateTime? timeCreated,
        string message = "",
        string machineName = "",
        long? recordId = null,
        long? keywords = null)
    {
        EventId = eventId;
        LogName = logName ?? "";
        ProviderName = providerName ?? "";
        Level = level;
        TimeCreated = timeCreated;
        Message = message ?? "";
        MachineName = machineName ?? "";
        RecordId = recordId;
        Keywords = keywords;
    }
}
