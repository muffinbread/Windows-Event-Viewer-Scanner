using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.ViewModels;

/// <summary>
/// The ViewModel for the main dashboard screen.
/// Holds the current grade, findings, scan state, and handles scan commands.
///
/// Uses CommunityToolkit.Mvvm source generators:
///   [ObservableProperty] → auto-generates a public property with change notifications
///   [RelayCommand] → auto-generates a command property for button binding
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IScanService _scanService;

    // --- Observable properties (auto-generated as public properties) ---

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(QuickScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeepScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportLogsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CtfSpeedImportCommand))]
    private bool _isScanning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportReportCommand))]
    private bool _hasScanned;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan your system";

    [ObservableProperty]
    private string _gradeDisplayName = "";

    [ObservableProperty]
    private string _gradeDescription = "";

    [ObservableProperty]
    private int _gradeScore;

    [ObservableProperty]
    private SolidColorBrush _gradeBrush = new(Colors.White);

    [ObservableProperty]
    private double _glowRadius = 15;

    [ObservableProperty]
    private bool _isHighGrade;

    [ObservableProperty]
    private bool _isMaxGrade;

    [ObservableProperty]
    private string _scanSummary = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private ScanResult? _lastScanResult;

    [ObservableProperty]
    private Finding? _selectedFinding;

    [ObservableProperty]
    private string _sessionModeHint = "";

    [ObservableProperty]
    private bool _showCtfEntityPanel;

    [ObservableProperty]
    private bool _ctfPivotsHasRows;

    [ObservableProperty]
    private bool _showCtfPivotsPlaceholder;

    public ObservableCollection<Finding> Findings { get; } = [];

    public ObservableCollection<ExtractedPivot> CtfExtractedPivots { get; } = [];

    public DashboardViewModel() : this(ScanService.CreateDefault())
    {
    }

    public DashboardViewModel(IScanService scanService)
    {
        _scanService = scanService;
    }

    partial void OnSelectedFindingChanged(Finding? value)
    {
        RefreshCtfExtractedPivots();
    }

    partial void OnLastScanResultChanged(ScanResult? value)
    {
        RefreshCtfExtractedPivots();
    }

    private void RefreshCtfExtractedPivots()
    {
        CtfExtractedPivots.Clear();
        var isCtf = LastScanResult?.AnalysisProfile == ScanAnalysisProfile.CtfSpeed;
        ShowCtfEntityPanel = isCtf && SelectedFinding != null;
        CtfPivotsHasRows = false;
        ShowCtfPivotsPlaceholder = false;

        if (!isCtf || SelectedFinding == null)
            return;

        foreach (var pivot in EventMessageEntityExtractor.ExtractFromFinding(SelectedFinding))
            CtfExtractedPivots.Add(pivot);

        CtfPivotsHasRows = CtfExtractedPivots.Count > 0;
        ShowCtfPivotsPlaceholder = CtfExtractedPivots.Count == 0;
    }

    // --- Commands ---

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task QuickScanAsync()
    {
        await RunScanAsync(ScanType.Quick);
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task DeepScanAsync()
    {
        await RunScanAsync(ScanType.Deep);
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ImportLogsAsync()
    {
        await ImportEvtxAsync(ScanAnalysisProfile.Standard, forCtfSpeed: false);
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task CtfSpeedImportAsync()
    {
        await ImportEvtxAsync(ScanAnalysisProfile.CtfSpeed, forCtfSpeed: true);
    }

    private async Task ImportEvtxAsync(ScanAnalysisProfile profile, bool forCtfSpeed)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = forCtfSpeed ? "CTF speed — import .evtx files" : "Import Event Viewer Logs",
            Filter = "Event Log Files (*.evtx)|*.evtx|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
            return;

        IsScanning = true;
        ErrorMessage = "";
        var fileCount = dialog.FileNames.Length;
        var fileWord = fileCount == 1 ? "file" : "files";
        StatusMessage = forCtfSpeed
            ? $"CTF triage: analyzing {fileCount} log {fileWord}..."
            : $"Analyzing {fileCount} imported log {fileWord}...";

        try
        {
            var result = await _scanService.RunFileScanAsync(dialog.FileNames, CancellationToken.None, profile);
            var doneVerb = forCtfSpeed ? "CTF triage complete" : "Import complete";
            DisplayResult(result, $"{doneVerb} \u2022 {result.TotalEventsAnalyzed:N0} events from {fileCount} {fileWord} analyzed in {result.Duration.TotalSeconds:F1}s");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Import was cancelled";
            SessionModeHint = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Import failed: {ex.Message}";
            StatusMessage = "Import failed \u2014 see error above";
            SessionModeHint = "";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void ExportReport()
    {
        if (LastScanResult == null)
            return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Report",
            Filter = "HTML Report (*.html)|*.html",
            FileName = $"EventScanner_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var html = ReportExporter.GenerateHtml(LastScanResult);
            System.IO.File.WriteAllText(dialog.FileName, html, System.Text.Encoding.UTF8);
            StatusMessage = $"Report exported to {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    private bool CanExport() => HasScanned && LastScanResult != null;

    private bool CanScan() => !IsScanning;

    // --- Core scan logic ---

    private async Task RunScanAsync(ScanType scanType)
    {
        IsScanning = true;
        ErrorMessage = "";
        SelectedFinding = null;
        SessionModeHint = "";
        StatusMessage = scanType == ScanType.Quick
            ? "Running Quick Scan (last 24 hours)..."
            : "Running Deep Scan (last 30 days)...";

        try
        {
            var result = await _scanService.RunScanAsync(scanType);
            DisplayResult(result, $"Scan complete \u2022 {result.TotalEventsAnalyzed:N0} events analyzed in {result.Duration.TotalSeconds:F1}s");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan was cancelled";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Scan failed: {ex.Message}";
            StatusMessage = "Scan failed \u2014 see error above";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void DisplayResult(ScanResult result, string statusText)
    {
        LastScanResult = result;
        GradeDisplayName = result.Grade.DisplayName;
        GradeDescription = result.Grade.Description;
        GradeScore = result.Grade.Score;
        GradeBrush = GetGradeBrush(result.Grade.Grade);
        GlowRadius = GetGlowRadius(result.Grade.Grade);
        IsHighGrade = result.Grade.Grade >= GradeLevel.S;
        IsMaxGrade = result.Grade.Grade == GradeLevel.SSSPlus;

        Findings.Clear();
        foreach (var finding in result.Findings)
            Findings.Add(finding);

        HasScanned = true;
        ScanSummary = BuildScanSummary(result);
        StatusMessage = statusText;
        SessionModeHint = result.AnalysisProfile == ScanAnalysisProfile.CtfSpeed
            ? "CTF speed: full detection and grading — findings are sorted by severity, how often they fired, and most recent activity so you can pivot quickly."
            : "";
    }

    private static string BuildScanSummary(ScanResult result)
    {
        if (result.Findings.Count == 0)
            return "No issues found \u2014 your system looks great!";

        var parts = new List<string>();
        var grade = result.Grade;

        if (grade.CriticalCount > 0) parts.Add($"{grade.CriticalCount} Critical");
        if (grade.HighCount > 0) parts.Add($"{grade.HighCount} High");
        if (grade.MediumCount > 0) parts.Add($"{grade.MediumCount} Medium");
        if (grade.LowCount > 0) parts.Add($"{grade.LowCount} Low");
        if (grade.InformationalCount > 0) parts.Add($"{grade.InformationalCount} Info");

        var findingWord = result.Findings.Count == 1 ? "finding" : "findings";
        return $"{result.Findings.Count} {findingWord}: {string.Join(", ", parts)}";
    }

    private static SolidColorBrush GetGradeBrush(GradeLevel level)
    {
        return level switch
        {
            GradeLevel.F => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
            GradeLevel.D => new SolidColorBrush(Color.FromRgb(255, 107, 53)),
            GradeLevel.C => new SolidColorBrush(Color.FromRgb(255, 165, 0)),
            GradeLevel.B => new SolidColorBrush(Color.FromRgb(154, 205, 50)),
            GradeLevel.A => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            GradeLevel.S => new SolidColorBrush(Color.FromRgb(0, 180, 216)),
            GradeLevel.SS => new SolidColorBrush(Color.FromRgb(138, 43, 226)),
            GradeLevel.SSS => new SolidColorBrush(Color.FromRgb(255, 215, 0)),
            GradeLevel.SSSPlus => new SolidColorBrush(Color.FromRgb(255, 215, 0)),
            _ => new SolidColorBrush(Colors.White)
        };
    }

    private static double GetGlowRadius(GradeLevel level)
    {
        return level switch
        {
            GradeLevel.F => 5,
            GradeLevel.D => 8,
            GradeLevel.C => 12,
            GradeLevel.B => 16,
            GradeLevel.A => 20,
            GradeLevel.S => 28,
            GradeLevel.SS => 35,
            GradeLevel.SSS => 42,
            GradeLevel.SSSPlus => 50,
            _ => 10
        };
    }
}
