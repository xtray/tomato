using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public class TimerRingGeometryTests
{
    [Theory]
    [InlineData(1F, -90F, 360F)]
    [InlineData(0.75F, 0F, 270F)]
    [InlineData(0.5F, 90F, 180F)]
    [InlineData(0.25F, 180F, 90F)]
    [InlineData(0F, 270F, 0F)]
    public void DescribeCountdownArc_ReturnsClockwiseEmptyGrowthGeometry(
        float remainingRatio,
        float expectedStartAngle,
        float expectedSweepAngle
    )
    {
        var arc = TimerRingGeometry.DescribeCountdownArc(remainingRatio);

        Assert.Equal(expectedStartAngle, arc.StartAngle);
        Assert.Equal(expectedSweepAngle, arc.SweepAngle);
    }

    [Theory]
    [InlineData(-1F, 270F, 0F)]
    [InlineData(2F, -90F, 360F)]
    public void DescribeCountdownArc_ClampsInputRatio(float remainingRatio, float expectedStartAngle, float expectedSweepAngle)
    {
        var arc = TimerRingGeometry.DescribeCountdownArc(remainingRatio);

        Assert.Equal(expectedStartAngle, arc.StartAngle);
        Assert.Equal(expectedSweepAngle, arc.SweepAngle);
    }
}
