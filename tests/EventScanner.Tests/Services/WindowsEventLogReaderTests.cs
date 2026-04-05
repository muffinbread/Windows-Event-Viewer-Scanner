using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.Tests.Services;

/// <summary>
/// Integration tests that read real Windows event logs.
/// These tests depend on the actual machine's event log state.
/// They verify the reader works correctly and handles edge cases.
/// </summary>
public class WindowsEventLogReaderTests
{
    private readonly WindowsEventLogReader _reader = new();

    [Fact]
    public void GetAccessibleLogNames_ReturnsAtLeastSystemAndApplication()
    {
        var logs = _reader.GetAccessibleLogNames();

        Assert.Contains("System", logs);
        Assert.Contains("Application", logs);
    }

    [Fact]
    public async Task ReadEventsAsync_SystemLog_ReturnsEvents()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-7);

        var events = await _reader.ReadEventsAsync("System", from, to);

        // Every Windows machine should have at least some System events
        // in the last 7 days.
        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task ReadEventsAsync_SystemLog_EventsHaveRequiredFields()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-1);

        var events = await _reader.ReadEventsAsync("System", from, to, maxEvents: 5);

        foreach (var evt in events)
        {
            Assert.Equal("System", evt.LogName);
            Assert.False(string.IsNullOrEmpty(evt.ProviderName),
                "Every event should have a provider name");
            Assert.NotNull(evt.TimeCreated);
        }
    }

    [Fact]
    public async Task ReadEventsAsync_RespectsMaxEvents()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-30);

        var events = await _reader.ReadEventsAsync("System", from, to, maxEvents: 3);

        Assert.True(events.Count <= 3,
            $"Expected at most 3 events but got {events.Count}");
    }

    [Fact]
    public async Task ReadEventsAsync_NonexistentLog_ReturnsEmpty()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-1);

        var events = await _reader.ReadEventsAsync("ThisLogDoesNotExist", from, to);

        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadEventsAsync_EmptyLogName_ReturnsEmpty()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-1);

        var events = await _reader.ReadEventsAsync("", from, to);

        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadEventsAsync_FutureTimeRange_ReturnsEmpty()
    {
        var from = DateTime.UtcNow.AddYears(10);
        var to = from.AddDays(1);

        var events = await _reader.ReadEventsAsync("System", from, to);

        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadEventsAsync_ApplicationLog_ReturnsEvents()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-7);

        var events = await _reader.ReadEventsAsync("Application", from, to);

        // Most machines should have Application log events in the last week.
        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task ReadEventsAsync_CanBeCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _reader.ReadEventsAsync("System",
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow,
                maxEvents: 50_000,
                cancellationToken: cts.Token));
    }
}
