using EventScanner.Models;

namespace EventScanner.Services;

/// <summary>
/// Orchestrates the full scan pipeline: read logs → analyze → grade.
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Runs a complete scan of the system's event logs.
    /// </summary>
    Task<ScanResult> RunScanAsync(
        ScanType scanType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes imported .evtx files from any machine.
    /// Reads all events from the files, runs detection rules, and computes a grade.
    /// </summary>
    /// <param name="analysisProfile">
    /// <see cref="ScanAnalysisProfile.CtfSpeed"/> re-sorts findings for quick triage;
    /// grading and rule coverage stay the same.
    /// </param>
    Task<ScanResult> RunFileScanAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken = default,
        ScanAnalysisProfile analysisProfile = ScanAnalysisProfile.Standard);
}
