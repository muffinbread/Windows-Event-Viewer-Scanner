using EventScanner.Models;

namespace EventScanner.Tests.Models;

public class EventLogEntryTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var time = new DateTime(2026, 4, 4, 12, 0, 0, DateTimeKind.Utc);
        var entry = new EventLogEntry(
            eventId: 7,
            logName: "System",
            providerName: "disk",
            level: EventLogLevel.Error,
            timeCreated: time,
            message: "The device has a bad block.",
            machineName: "MYPC",
            recordId: 12345,
            keywords: -9218868437227405312);

        Assert.Equal(7, entry.EventId);
        Assert.Equal("System", entry.LogName);
        Assert.Equal("disk", entry.ProviderName);
        Assert.Equal(EventLogLevel.Error, entry.Level);
        Assert.Equal(time, entry.TimeCreated);
        Assert.Equal("The device has a bad block.", entry.Message);
        Assert.Equal("MYPC", entry.MachineName);
        Assert.Equal(12345L, entry.RecordId);
        Assert.Equal(-9218868437227405312L, entry.Keywords);
    }

    [Fact]
    public void Constructor_NullsDefaultToEmptyStrings()
    {
        var entry = new EventLogEntry(
            eventId: 1,
            logName: null!,
            providerName: null!,
            level: EventLogLevel.Informational,
            timeCreated: null);

        Assert.Equal("", entry.LogName);
        Assert.Equal("", entry.ProviderName);
        Assert.Equal("", entry.Message);
        Assert.Equal("", entry.MachineName);
        Assert.Null(entry.TimeCreated);
        Assert.Null(entry.RecordId);
        Assert.Null(entry.Keywords);
    }

    [Fact]
    public void Constructor_DefaultMessageIsEmpty()
    {
        var entry = new EventLogEntry(
            eventId: 100,
            logName: "Application",
            providerName: "TestApp",
            level: EventLogLevel.Warning,
            timeCreated: DateTime.UtcNow);

        Assert.Equal("", entry.Message);
    }

    [Theory]
    [InlineData(EventLogLevel.LogAlways)]
    [InlineData(EventLogLevel.Critical)]
    [InlineData(EventLogLevel.Error)]
    [InlineData(EventLogLevel.Warning)]
    [InlineData(EventLogLevel.Informational)]
    [InlineData(EventLogLevel.Verbose)]
    public void AllEventLogLevels_AreValid(EventLogLevel level)
    {
        var entry = new EventLogEntry(
            eventId: 1,
            logName: "System",
            providerName: "Test",
            level: level,
            timeCreated: DateTime.UtcNow);

        Assert.Equal(level, entry.Level);
    }
}
