using EventScanner.Models;

namespace EventScanner.Tests.Models;

public class SystemGradeTests
{
    [Fact]
    public void Constructor_SetsGradeFromScore()
    {
        var grade = new SystemGrade(score: 85);

        Assert.Equal(85, grade.Score);
        Assert.Equal(GradeLevel.S, grade.Grade);
        Assert.Equal("S", grade.DisplayName);
    }

    [Fact]
    public void Constructor_ClampsScoreBelow0()
    {
        var grade = new SystemGrade(score: -10);

        Assert.Equal(0, grade.Score);
        Assert.Equal(GradeLevel.F, grade.Grade);
    }

    [Fact]
    public void Constructor_ClampsScoreAbove100()
    {
        var grade = new SystemGrade(score: 150);

        Assert.Equal(100, grade.Score);
        Assert.Equal(GradeLevel.SSSPlus, grade.Grade);
    }

    [Fact]
    public void TotalFindings_SumsAllSeverityCounts()
    {
        var grade = new SystemGrade(
            score: 70,
            criticalCount: 1,
            highCount: 3,
            mediumCount: 5,
            lowCount: 8,
            informationalCount: 12);

        Assert.Equal(29, grade.TotalFindings);
    }

    [Fact]
    public void TotalFindings_IsZeroWhenNoCounts()
    {
        var grade = new SystemGrade(score: 100);

        Assert.Equal(0, grade.TotalFindings);
    }

    [Fact]
    public void Constructor_NegativeCounts_ClampedToZero()
    {
        var grade = new SystemGrade(
            score: 50,
            criticalCount: -5,
            highCount: -1);

        Assert.Equal(0, grade.CriticalCount);
        Assert.Equal(0, grade.HighCount);
        Assert.Equal(0, grade.TotalFindings);
    }

    [Fact]
    public void Constructor_SetsGradedAtToUtcNow_WhenNotProvided()
    {
        var before = DateTime.UtcNow;
        var grade = new SystemGrade(score: 50);
        var after = DateTime.UtcNow;

        Assert.InRange(grade.GradedAt, before, after);
    }

    [Fact]
    public void Constructor_UsesProvidedGradedAt()
    {
        var timestamp = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var grade = new SystemGrade(score: 50, gradedAt: timestamp);

        Assert.Equal(timestamp, grade.GradedAt);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var grade = new SystemGrade(score: 75);

        Assert.False(string.IsNullOrWhiteSpace(grade.Description));
    }

    [Theory]
    [InlineData(0, "F")]
    [InlineData(50, "B")]
    [InlineData(80, "S")]
    [InlineData(99, "SSS+")]
    [InlineData(100, "SSS+")]
    public void DisplayName_MatchesScoreGrade(int score, string expectedDisplay)
    {
        var grade = new SystemGrade(score: score);
        Assert.Equal(expectedDisplay, grade.DisplayName);
    }
}
