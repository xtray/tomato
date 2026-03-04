namespace Tomato.WindowsCore;

public readonly record struct TimerRingArc(float StartAngle, float SweepAngle);

public static class TimerRingGeometry
{
    private const float FullCircleDegrees = 360F;
    private const float TopClockAngle = -90F;

    public static TimerRingArc DescribeCountdownArc(float remainingRatio)
    {
        var clampedRatio = Math.Clamp(remainingRatio, 0F, 1F);
        var elapsedRatio = 1F - clampedRatio;
        var startAngle = TopClockAngle + (elapsedRatio * FullCircleDegrees);
        var sweepAngle = clampedRatio * FullCircleDegrees;
        return new TimerRingArc(startAngle, sweepAngle);
    }
}
