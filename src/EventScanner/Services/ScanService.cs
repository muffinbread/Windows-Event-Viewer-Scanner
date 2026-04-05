using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Orchestrates the full scan pipeline:
/// 1. Determines which logs to read and the time range
/// 2. Reads raw events from Windows Event Viewer
/// 3. Runs detection rules to produce findings
/// 4. Computes the overall system grade
/// 5. Packages everything into a ScanResult
/// </summary>
public sealed class ScanService : IScanService
{
    private readonly IEventLogReader _logReader;
    private readonly IEventAnalyzer _analyzer;
    private readonly IGradingEngine _gradingEngine;

    public ScanService(
        IEventLogReader logReader,
        IEventAnalyzer analyzer,
        IGradingEngine gradingEngine)
    {
        _logReader = logReader ?? throw new ArgumentNullException(nameof(logReader));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _gradingEngine = gradingEngine ?? throw new ArgumentNullException(nameof(gradingEngine));
    }

    /// <summary>
    /// Creates a ScanService with all the default built-in components.
    /// </summary>
    public static ScanService CreateDefault()
    {
        return new ScanService(
            new WindowsEventLogReader(),
            EventAnalyzer.CreateWithDefaultRules(),
            new GradingEngine());
    }

    public async Task<ScanResult> RunScanAsync(
        ScanType scanType,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;

        var scanRangeEnd = DateTime.UtcNow;
        var scanRangeStart = scanType == ScanType.Quick
            ? scanRangeEnd.AddHours(-24)
            : scanRangeEnd.AddDays(-30);

        var accessibleLogs = _logReader.GetAccessibleLogNames();

        var allEvents = new List<EventLogEntry>();
        foreach (var logName in accessibleLogs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var events = await _logReader.ReadEventsAsync(
                logName, scanRangeStart, scanRangeEnd,
                cancellationToken: cancellationToken);

            allEvents.AddRange(events);
        }

        var findings = _analyzer.Analyze(allEvents);

        var grade = _gradingEngine.ComputeGrade(findings);

        var completedAt = DateTime.UtcNow;

        return new ScanResult(
            scanType: scanType,
            startedAt: startedAt,
            completedAt: completedAt,
            scanRangeStart: scanRangeStart,
            scanRangeEnd: scanRangeEnd,
            scannedLogs: accessibleLogs,
            findings: findings,
            grade: grade,
            totalEventsAnalyzed: allEvents.Count,
            analysisProfile: ScanAnalysisProfile.Standard);
    }

    public async Task<ScanResult> RunFileScanAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken = default,
        ScanAnalysisProfile analysisProfile = ScanAnalysisProfile.Standard)
    {
        if (filePaths == null || filePaths.Count == 0)
            throw new ArgumentException("At least one file path is required.", nameof(filePaths));

        var startedAt = DateTime.UtcNow;

        var allEvents = new List<EventLogEntry>();
        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var events = await _logReader.ReadEventsFromFileAsync(
                filePath, cancellationToken: cancellationToken);

            allEvents.AddRange(events);
        }

        var findings = _analyzer.Analyze(allEvents);
        if (analysisProfile == ScanAnalysisProfile.CtfSpeed)
            findings = CtfFindingTriage.OrderForSpeed(findings);

        var grade = _gradingEngine.ComputeGrade(findings);
        var completedAt = DateTime.UtcNow;

        var scanRangeStart = allEvents.Count > 0
            ? allEvents.Where(e => e.TimeCreated.HasValue).Min(e => e.TimeCreated!.Value)
            : startedAt;
        var scanRangeEnd = allEvents.Count > 0
            ? allEvents.Where(e => e.TimeCreated.HasValue).Max(e => e.TimeCreated!.Value)
            : completedAt;

        var logNames = allEvents
            .Select(e => e.LogName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ScanResult(
            scanType: ScanType.Import,
            startedAt: startedAt,
            completedAt: completedAt,
            scanRangeStart: scanRangeStart,
            scanRangeEnd: scanRangeEnd,
            scannedLogs: logNames,
            findings: findings,
            grade: grade,
            totalEventsAnalyzed: allEvents.Count,
            analysisProfile: analysisProfile);
    }
}
