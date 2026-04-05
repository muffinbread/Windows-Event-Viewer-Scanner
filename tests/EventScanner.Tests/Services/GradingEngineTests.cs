using EventScanner.Models;
using EventScanner.Services;

namespace EventScanner.Tests.Services;

public class GradingEngineTests
{
    private readonly GradingEngine _engine = new();

    private static Finding CreateFinding(
        Severity severity = Severity.Medium,
        Confidence confidence = Confidence.High,
        bool isSuppressed = false)
    {
        var finding = new Finding(
            title: $"Test {severity} Finding",
            description: "Test finding for grading",
            severity: severity,
            sourceLog: "System",
            ruleId: "TEST-001",
            confidence: confidence);

        finding.IsSuppressed = isSuppressed;
        return finding;
    }

    // --- Clean system ---

    [Fact]
    public void ComputeGrade_NoFindings_Returns100_SSSPlus()
    {
        var grade = _engine.ComputeGrade(Array.Empty<Finding>());

        Assert.Equal(100, grade.Score);
        Assert.Equal(GradeLevel.SSSPlus, grade.Grade);
        Assert.Equal(0, grade.TotalFindings);
    }

    [Fact]
    public void ComputeGrade_NullFindings_Returns100_SSSPlus()
    {
        var grade = _engine.ComputeGrade(null!);

        Assert.Equal(100, grade.Score);
        Assert.Equal(GradeLevel.SSSPlus, grade.Grade);
    }

    // --- Single finding, each severity ---

    [Fact]
    public void ComputeGrade_OneCritical_Deducts30()
    {
        var findings = new[] { CreateFinding(Severity.Critical) };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(70, grade.Score);
        Assert.Equal(GradeLevel.A, grade.Grade);
        Assert.Equal(1, grade.CriticalCount);
    }

    [Fact]
    public void ComputeGrade_OneHigh_Deducts15()
    {
        var findings = new[] { CreateFinding(Severity.High) };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(85, grade.Score);
        Assert.Equal(GradeLevel.S, grade.Grade);
        Assert.Equal(1, grade.HighCount);
    }

    [Fact]
    public void ComputeGrade_OneMedium_Deducts8()
    {
        var findings = new[] { CreateFinding(Severity.Medium) };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(92, grade.Score);
        Assert.Equal(GradeLevel.SS, grade.Grade);
        Assert.Equal(1, grade.MediumCount);
    }

    [Fact]
    public void ComputeGrade_OneLow_Deducts3()
    {
        var findings = new[] { CreateFinding(Severity.Low) };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(97, grade.Score);
        Assert.Equal(GradeLevel.SSS, grade.Grade);
        Assert.Equal(1, grade.LowCount);
    }

    [Fact]
    public void ComputeGrade_OneInformational_Deducts1()
    {
        var findings = new[] { CreateFinding(Severity.Informational) };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(99, grade.Score);
        Assert.Equal(GradeLevel.SSSPlus, grade.Grade);
        Assert.Equal(1, grade.InformationalCount);
    }

    // --- Multiple findings ---

    [Fact]
    public void ComputeGrade_MixedFindings_DeductsCorrectly()
    {
        var findings = new[]
        {
            CreateFinding(Severity.High),           // -15
            CreateFinding(Severity.Medium),          // -8
            CreateFinding(Severity.Low),             // -3
            CreateFinding(Severity.Informational),   // -1
        };
        // Total deduction: 27, Score: 73

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(73, grade.Score);
        Assert.Equal(GradeLevel.A, grade.Grade);
        Assert.Equal(0, grade.CriticalCount);
        Assert.Equal(1, grade.HighCount);
        Assert.Equal(1, grade.MediumCount);
        Assert.Equal(1, grade.LowCount);
        Assert.Equal(1, grade.InformationalCount);
    }

    [Fact]
    public void ComputeGrade_TwoCriticals_ScoreIs40()
    {
        var findings = new[]
        {
            CreateFinding(Severity.Critical),   // -30
            CreateFinding(Severity.Critical),   // -30
        };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(40, grade.Score);
        Assert.Equal(GradeLevel.C, grade.Grade);
        Assert.Equal(2, grade.CriticalCount);
    }

    // --- Score floor at 0 ---

    [Fact]
    public void ComputeGrade_MassiveDeductions_ClampsToZero()
    {
        var findings = Enumerable.Range(0, 10)
            .Select(_ => CreateFinding(Severity.Critical))
            .ToList();
        // 10 * 30 = 300, but score is clamped to 0

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(0, grade.Score);
        Assert.Equal(GradeLevel.F, grade.Grade);
    }

    // --- Suppressed findings ---

    [Fact]
    public void ComputeGrade_SuppressedFindingsAreExcluded()
    {
        var findings = new[]
        {
            CreateFinding(Severity.Critical, isSuppressed: true),   // excluded
            CreateFinding(Severity.Low),                             // -3
        };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(97, grade.Score);
        Assert.Equal(0, grade.CriticalCount);
        Assert.Equal(1, grade.LowCount);
    }

    [Fact]
    public void ComputeGrade_AllSuppressed_Returns100()
    {
        var findings = new[]
        {
            CreateFinding(Severity.Critical, isSuppressed: true),
            CreateFinding(Severity.High, isSuppressed: true),
        };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(100, grade.Score);
        Assert.Equal(GradeLevel.SSSPlus, grade.Grade);
    }

    // --- Confidence multiplier ---

    [Fact]
    public void ComputeGrade_MediumConfidence_ReducesDeduction()
    {
        var findings = new[]
        {
            CreateFinding(Severity.Critical, confidence: Confidence.Medium),
        };
        // 30 * 0.7 = 21, Score: 100 - 21 = 79

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(79, grade.Score);
        Assert.Equal(GradeLevel.A, grade.Grade);
    }

    [Fact]
    public void ComputeGrade_LowConfidence_ReducesDeductionMore()
    {
        var findings = new[]
        {
            CreateFinding(Severity.Critical, confidence: Confidence.Low),
        };
        // 30 * 0.4 = 12, Score: 100 - 12 = 88

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(88, grade.Score);
        Assert.Equal(GradeLevel.SS, grade.Grade);
    }

    [Fact]
    public void ComputeGrade_HighConfidence_FullDeduction()
    {
        var findings = new[]
        {
            CreateFinding(Severity.High, confidence: Confidence.High),
        };
        // 15 * 1.0 = 15, Score: 85

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(85, grade.Score);
    }

    // --- Grade boundary verification ---

    [Fact]
    public void ComputeGrade_OnlyInformational_CanStillGetSSSPlus()
    {
        var findings = new[] { CreateFinding(Severity.Informational) };

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(GradeLevel.SSSPlus, grade.Grade);
    }

    [Fact]
    public void ComputeGrade_ThreeInformational_StillSSS()
    {
        var findings = Enumerable.Range(0, 3)
            .Select(_ => CreateFinding(Severity.Informational))
            .ToList();
        // 3 * 1 = 3, Score: 97

        var grade = _engine.ComputeGrade(findings);

        Assert.Equal(97, grade.Score);
        Assert.Equal(GradeLevel.SSS, grade.Grade);
    }

    [Fact]
    public void ComputeGrade_SetsGradedAtTimestamp()
    {
        var before = DateTime.UtcNow;
        var grade = _engine.ComputeGrade(Array.Empty<Finding>());
        var after = DateTime.UtcNow;

        Assert.InRange(grade.GradedAt, before, after);
    }
}
