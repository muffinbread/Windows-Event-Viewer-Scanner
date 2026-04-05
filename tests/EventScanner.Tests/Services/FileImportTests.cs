using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.Tests.Services;

public class FileImportTests
{
    private readonly WindowsEventLogReader _reader = new();

    [Fact]
    public async Task ReadEventsFromFileAsync_NonexistentFile_ReturnsEmpty()
    {
        var events = await _reader.ReadEventsFromFileAsync(@"C:\nonexistent\fake.evtx");

        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadEventsFromFileAsync_NullPath_ReturnsEmpty()
    {
        var events = await _reader.ReadEventsFromFileAsync(null!);

        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadEventsFromFileAsync_EmptyPath_ReturnsEmpty()
    {
        var events = await _reader.ReadEventsFromFileAsync("");

        Assert.Empty(events);
    }

    [Fact]
    public async Task ReadEventsFromFileAsync_InvalidFile_ReturnsEmpty()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "this is not an evtx file");

            var events = await _reader.ReadEventsFromFileAsync(tempFile);

            Assert.Empty(events);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RunFileScanAsync_EmptyFilePaths_Throws()
    {
        var service = ScanService.CreateDefault();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RunFileScanAsync(Array.Empty<string>()));
    }

    [Fact]
    public async Task RunFileScanAsync_NullFilePaths_Throws()
    {
        var service = ScanService.CreateDefault();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RunFileScanAsync(null!));
    }

    [Fact]
    public async Task RunFileScanAsync_NonexistentFiles_ProducesCleanGrade()
    {
        var service = ScanService.CreateDefault();

        var result = await service.RunFileScanAsync(new[] { @"C:\nonexistent\fake.evtx" });

        Assert.Equal(ScanType.Import, result.ScanType);
        Assert.Equal(100, result.Grade.Score);
        Assert.Equal(GradeLevel.SSSPlus, result.Grade.Grade);
        Assert.Empty(result.Findings);
        Assert.Equal(0, result.TotalEventsAnalyzed);
    }

    [Fact]
    public async Task RunFileScanAsync_CtfSpeed_SetsProfile()
    {
        var service = ScanService.CreateDefault();

        var result = await service.RunFileScanAsync(
            new[] { @"C:\nonexistent\fake.evtx" },
            analysisProfile: ScanAnalysisProfile.CtfSpeed);

        Assert.Equal(ScanAnalysisProfile.CtfSpeed, result.AnalysisProfile);
        Assert.Equal(ScanType.Import, result.ScanType);
    }

    [Fact]
    public void ScanType_Import_Exists()
    {
        var importType = ScanType.Import;
        Assert.Equal(ScanType.Import, importType);
    }
}
