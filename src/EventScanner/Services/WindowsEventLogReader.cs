using System.Diagnostics.Eventing.Reader;
using System.IO;
using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Reads real Windows Event Viewer logs using the built-in .NET API.
/// Handles permission errors, missing logs, and malformed events gracefully.
/// </summary>
public sealed class WindowsEventLogReader : IEventLogReader
{
    private static readonly string[] DefaultLogNames = ["System", "Application", "Security", "Windows PowerShell"];

    public async Task<IReadOnlyList<EventLogEntry>> ReadEventsAsync(
        string logName,
        DateTime from,
        DateTime to,
        int maxEvents = 10_000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(logName))
            return Array.Empty<EventLogEntry>();

        maxEvents = Math.Clamp(maxEvents, 1, 50_000);

        return await Task.Run(() => ReadEventsInternal(logName, from, to, maxEvents, cancellationToken),
            cancellationToken);
    }

    public async Task<IReadOnlyList<EventLogEntry>> ReadEventsFromFileAsync(
        string filePath,
        int maxEvents = 50_000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Array.Empty<EventLogEntry>();

        maxEvents = Math.Clamp(maxEvents, 1, 100_000);

        return await Task.Run(
            () => ReadEventsFromFileInternal(filePath, maxEvents, cancellationToken),
            cancellationToken);
    }

    public IReadOnlyList<string> GetAccessibleLogNames()
    {
        var accessible = new List<string>();

        foreach (var logName in DefaultLogNames)
        {
            if (CanAccessLog(logName))
                accessible.Add(logName);
        }

        return accessible;
    }

    private List<EventLogEntry> ReadEventsInternal(
        string logName,
        DateTime from,
        DateTime to,
        int maxEvents,
        CancellationToken cancellationToken)
    {
        var results = new List<EventLogEntry>();

        try
        {
            var fromUtc = from.ToUniversalTime();
            var toUtc = to.ToUniversalTime();

            var queryText = $"*[System[TimeCreated[@SystemTime >= '{fromUtc:o}' and @SystemTime <= '{toUtc:o}']]]";
            var query = new EventLogQuery(logName, PathType.LogName, queryText)
            {
                ReverseDirection = true
            };

            using var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(query);

            EventRecord? record;
            while ((record = reader.ReadEvent()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (record)
                {
                    var entry = ConvertToEntry(record, logName);
                    if (entry != null)
                        results.Add(entry);

                    if (results.Count >= maxEvents)
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // Security log or other protected log without admin rights.
            // Return empty — the caller will see 0 events and can inform the user.
        }
        catch (EventLogNotFoundException)
        {
            // The requested log does not exist on this machine.
        }
        catch (EventLogReadingException)
        {
            // The log exists but could not be read (corrupted, locked, etc.).
        }

        return results;
    }

    private static EventLogEntry? ConvertToEntry(EventRecord record, string logName)
    {
        try
        {
            var level = ConvertLevel(record.Level);

            string message;
            try
            {
                message = record.FormatDescription() ?? "";
            }
            catch
            {
                message = "";
            }

            return new EventLogEntry(
                eventId: record.Id,
                logName: logName,
                providerName: record.ProviderName ?? "",
                level: level,
                timeCreated: record.TimeCreated,
                message: message,
                machineName: record.MachineName ?? "",
                recordId: record.RecordId,
                keywords: record.Keywords);
        }
        catch
        {
            // If a single event is malformed, skip it rather than crashing the whole scan.
            return null;
        }
    }

    private static EventLogLevel ConvertLevel(byte? level)
    {
        return level switch
        {
            0 => EventLogLevel.LogAlways,
            1 => EventLogLevel.Critical,
            2 => EventLogLevel.Error,
            3 => EventLogLevel.Warning,
            4 => EventLogLevel.Informational,
            5 => EventLogLevel.Verbose,
            _ => EventLogLevel.Informational
        };
    }

    private List<EventLogEntry> ReadEventsFromFileInternal(
        string filePath,
        int maxEvents,
        CancellationToken cancellationToken)
    {
        var results = new List<EventLogEntry>();

        try
        {
            var query = new EventLogQuery(filePath, PathType.FilePath);
            using var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(query);

            EventRecord? record;
            while ((record = reader.ReadEvent()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (record)
                {
                    var logName = record.LogName ?? Path.GetFileNameWithoutExtension(filePath);
                    var entry = ConvertToEntry(record, logName);
                    if (entry != null)
                        results.Add(entry);

                    if (results.Count >= maxEvents)
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Invalid file, corrupted, wrong format, etc.
            // Return whatever was read so far (may be empty).
        }

        return results;
    }

    private static bool CanAccessLog(string logName)
    {
        try
        {
            using var session = new EventLogSession();
            var info = session.GetLogInformation(logName, PathType.LogName);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
