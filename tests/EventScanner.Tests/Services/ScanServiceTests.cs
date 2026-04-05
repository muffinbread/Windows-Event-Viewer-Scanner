using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.Tests.Services;

public class ScanServiceTests
{
    [Fact]
    public async Task RunScanAsync_QuickScan_ReturnsResult()
    {
        var service = ScanService.CreateDefault();

        var result = await service.RunScanAsync(ScanType.Quick);

        Assert.NotNull(result);
        Assert.Equal(ScanType.Quick, result.ScanType);
        Assert.NotNull(result.Grade);
        Assert.NotNull(result.Findings);
        Assert.True(result.TotalEventsAnalyzed >= 0);
        Assert.True(result.Duration.TotalSeconds >= 0);
    }

    [Fact]
    public async Task RunScanAsync_QuickScan_ScanRange_Is24Hours()
    {
        var before = DateTime.UtcNow;
        var service = ScanService.CreateDefault();

        var result = await service.RunScanAsync(ScanType.Quick);

        var expectedRangeHours = (result.ScanRangeEnd - result.ScanRangeStart).TotalHours;
        Assert.InRange(expectedRangeHours, 23.9, 24.1);
    }

    [Fact]
    public async Task RunScanAsync_DeepScan_ScanRange_Is30Days()
    {
        var service = ScanService.CreateDefault();

        var result = await service.RunScanAsync(ScanType.Deep);

        var expectedRangeDays = (result.ScanRangeEnd - result.ScanRangeStart).TotalDays;
        Assert.InRange(expectedRangeDays, 29.9, 30.1);
    }

    [Fact]
    public async Task RunScanAsync_ScannedLogs_ContainsSystemAndApplication()
    {
        var service = ScanService.CreateDefault();

        var result = await service.RunScanAsync(ScanType.Quick);

        Assert.Contains("System", result.ScannedLogs);
        Assert.Contains("Application", result.ScannedLogs);
    }

    [Fact]
    public async Task RunScanAsync_Grade_ScoreIsInValidRange()
    {
        var service = ScanService.CreateDefault();

        var result = await service.RunScanAsync(ScanType.Quick);

        Assert.InRange(result.Grade.Score, 0, 100);
    }

    [Fact]
    public async Task RunScanAsync_CanBeCancelled()
    {
        var service = ScanService.CreateDefault();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RunScanAsync(ScanType.Deep, cts.Token));
    }

    [Fact]
    public async Task RunScanAsync_CompletedAt_IsAfterStartedAt()
    {
        var service = ScanService.CreateDefault();

        var result = await service.RunScanAsync(ScanType.Quick);

        Assert.True(result.CompletedAt >= result.StartedAt);
    }
}
