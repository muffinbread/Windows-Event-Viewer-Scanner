using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.Tests.Services;

public class EventMessageEntityExtractorTests
{
    [Fact]
    public void ExtractFromFinding_FindsIpv4()
    {
        var f = new Finding(
            title: "T",
            description: "D",
            severity: Severity.Low,
            sourceLog: "Security",
            ruleId: "X",
            matchedEvents:
            [
                new EventLogEntry(4625, "Security", "Microsoft-Windows-Security-Auditing",
                    EventLogLevel.Informational, DateTime.UtcNow,
                    "Source network address: 192.168.50.10")
            ]);

        var pivots = EventMessageEntityExtractor.ExtractFromFinding(f);

        Assert.Contains(pivots, p => p.Category == "IPv4" && p.Value == "192.168.50.10");
    }

    [Fact]
    public void ExtractFromFinding_FindsAccountPattern()
    {
        var f = new Finding(
            title: "T",
            description: "D",
            severity: Severity.Low,
            sourceLog: "Security",
            ruleId: "X",
            matchedEvents:
            [
                new EventLogEntry(4624, "Security", "Microsoft-Windows-Security-Auditing",
                    EventLogLevel.Informational, DateTime.UtcNow,
                    "Account Name: CONTOSO\\alice was used.")
            ]);

        var pivots = EventMessageEntityExtractor.ExtractFromFinding(f);

        Assert.Contains(pivots, p => p.Category == "Account" && p.Value.Equals("CONTOSO\\alice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExtractFromFinding_FindsDrivePath()
    {
        var f = new Finding(
            title: "T",
            description: "D",
            severity: Severity.Low,
            sourceLog: "System",
            ruleId: "X",
            matchedEvents:
            [
                new EventLogEntry(1, "System", "x", EventLogLevel.Error, DateTime.UtcNow,
                    "Could not read C:\\Temp\\payload.exe , error 5.")
            ]);

        var pivots = EventMessageEntityExtractor.ExtractFromFinding(f);

        Assert.Contains(pivots, p => p is { Category: "Path", Value: @"C:\Temp\payload.exe" });
    }

    [Fact]
    public void ExtractFromFinding_IncludesComputerFromMachineName()
    {
        var f = new Finding(
            title: "T",
            description: "D",
            severity: Severity.Low,
            sourceLog: "System",
            ruleId: "X",
            matchedEvents:
            [
                new EventLogEntry(1, "System", "x", EventLogLevel.Error, DateTime.UtcNow,
                    message: "", machineName: "WIN-CTF-01")
            ]);

        var pivots = EventMessageEntityExtractor.ExtractFromFinding(f);

        Assert.Contains(pivots, p => p.Category == "Computer" && p.Value == "WIN-CTF-01");
    }

    [Fact]
    public void ExtractFromFinding_DedupesSameValue()
    {
        var f = new Finding(
            title: "T",
            description: "D",
            severity: Severity.Low,
            sourceLog: "System",
            ruleId: "X",
            matchedEvents:
            [
                new EventLogEntry(1, "System", "x", EventLogLevel.Error, DateTime.UtcNow, "10.0.0.7 and 10.0.0.7"),
                new EventLogEntry(2, "System", "x", EventLogLevel.Error, DateTime.UtcNow, "from 10.0.0.7")
            ]);

        var pivots = EventMessageEntityExtractor.ExtractFromFinding(f)
            .Where(p => p.Category == "IPv4")
            .ToList();

        Assert.Single(pivots);
    }
}
