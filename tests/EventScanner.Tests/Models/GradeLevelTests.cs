using EventScanner.Models;

namespace EventScanner.Tests.Models;

public class GradeLevelTests
{
    [Theory]
    [InlineData(0, GradeLevel.F)]
    [InlineData(10, GradeLevel.F)]
    [InlineData(19, GradeLevel.F)]
    [InlineData(20, GradeLevel.D)]
    [InlineData(27, GradeLevel.D)]
    [InlineData(34, GradeLevel.D)]
    [InlineData(35, GradeLevel.C)]
    [InlineData(42, GradeLevel.C)]
    [InlineData(49, GradeLevel.C)]
    [InlineData(50, GradeLevel.B)]
    [InlineData(57, GradeLevel.B)]
    [InlineData(64, GradeLevel.B)]
    [InlineData(65, GradeLevel.A)]
    [InlineData(72, GradeLevel.A)]
    [InlineData(79, GradeLevel.A)]
    [InlineData(80, GradeLevel.S)]
    [InlineData(84, GradeLevel.S)]
    [InlineData(87, GradeLevel.S)]
    [InlineData(88, GradeLevel.SS)]
    [InlineData(90, GradeLevel.SS)]
    [InlineData(93, GradeLevel.SS)]
    [InlineData(94, GradeLevel.SSS)]
    [InlineData(96, GradeLevel.SSS)]
    [InlineData(98, GradeLevel.SSS)]
    [InlineData(99, GradeLevel.SSSPlus)]
    [InlineData(100, GradeLevel.SSSPlus)]
    public void FromScore_ReturnsCorrectGrade(int score, GradeLevel expected)
    {
        var result = GradeLevelExtensions.FromScore(score);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-50, GradeLevel.F)]
    [InlineData(-1, GradeLevel.F)]
    [InlineData(101, GradeLevel.SSSPlus)]
    [InlineData(999, GradeLevel.SSSPlus)]
    public void FromScore_ClampsOutOfRangeValues(int score, GradeLevel expected)
    {
        var result = GradeLevelExtensions.FromScore(score);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(GradeLevel.F, "F")]
    [InlineData(GradeLevel.D, "D")]
    [InlineData(GradeLevel.C, "C")]
    [InlineData(GradeLevel.B, "B")]
    [InlineData(GradeLevel.A, "A")]
    [InlineData(GradeLevel.S, "S")]
    [InlineData(GradeLevel.SS, "SS")]
    [InlineData(GradeLevel.SSS, "SSS")]
    [InlineData(GradeLevel.SSSPlus, "SSS+")]
    public void ToDisplayName_ReturnsCorrectName(GradeLevel grade, string expected)
    {
        Assert.Equal(expected, grade.ToDisplayName());
    }

    [Fact]
    public void ToDescription_ReturnsNonEmptyForAllGrades()
    {
        foreach (GradeLevel grade in Enum.GetValues<GradeLevel>())
        {
            var description = grade.ToDescription();
            Assert.False(string.IsNullOrWhiteSpace(description),
                $"Grade {grade} should have a non-empty description");
        }
    }

    [Fact]
    public void GradeLevels_AreOrderedFromWorstToBest()
    {
        Assert.True(GradeLevel.F < GradeLevel.D);
        Assert.True(GradeLevel.D < GradeLevel.C);
        Assert.True(GradeLevel.C < GradeLevel.B);
        Assert.True(GradeLevel.B < GradeLevel.A);
        Assert.True(GradeLevel.A < GradeLevel.S);
        Assert.True(GradeLevel.S < GradeLevel.SS);
        Assert.True(GradeLevel.SS < GradeLevel.SSS);
        Assert.True(GradeLevel.SSS < GradeLevel.SSSPlus);
    }
}
