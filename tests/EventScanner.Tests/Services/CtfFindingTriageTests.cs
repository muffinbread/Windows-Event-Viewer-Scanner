using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.Tests.Services;

public class CtfFindingTriageTests
{
    private static Finding F(string title, Severity sev, int count, DateTime? last) =>
        new(
            title: title,
            description: "d",
            severity: sev,
            sourceLog: "System",
            ruleId: $"RULE-{title}",
            occurrenceCount: count,
            lastOccurrence: last);

    [Fact]
    public void OrderForSpeed_PutsHigherSeverityFirst()
    {
        var low = F("Low item", Severity.Low, 99, DateTime.UtcNow);
        var critical = F("Crit item", Severity.Critical, 1, DateTime.UtcNow.AddDays(-1));

        var ordered = CtfFindingTriage.OrderForSpeed(new[] { low, critical });

        Assert.Equal(Severity.Critical, ordered[0].Severity);
        Assert.Equal(Severity.Low, ordered[1].Severity);
    }

    [Fact]
    public void OrderForSpeed_SameSeverity_SortsByOccurrenceThenLastTime()
    {
        var t1 = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
        var fewer = F("B", Severity.High, 2, t2);
        var more = F("A", Severity.High, 10, t1);

        var ordered = CtfFindingTriage.OrderForSpeed(new[] { fewer, more });

        Assert.Equal("A", ordered[0].Title);
        Assert.Equal("B", ordered[1].Title);
    }

    [Fact]
    public void OrderForSpeed_SameSeverityAndCount_UsesLatestLastOccurrence()
    {
        var older = F("X", Severity.Medium, 5, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = F("Y", Severity.Medium, 5, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        var ordered = CtfFindingTriage.OrderForSpeed(new[] { older, newer });

        Assert.Equal("Y", ordered[0].Title);
    }

    [Fact]
    public void OrderForSpeed_Empty_ReturnsEmpty()
    {
        Assert.Empty(CtfFindingTriage.OrderForSpeed(Array.Empty<Finding>()));
    }
}
