namespace Z21Sniffer.Presentation.Timeline;

public readonly record struct BarSpan(double X, double Width, double FullDurationSeconds);

public sealed class BarGeometry
{
    public double TimeToX(DateTimeOffset start, DateTimeOffset end, double width, DateTimeOffset time)
    {
        var span = (end - start).TotalSeconds;
        if (span <= 0) return 0;
        return (time - start).TotalSeconds / span * width;
    }

    public BarSpan? Compute(
        DateTimeOffset viewportStart,
        DateTimeOffset viewportEnd,
        double width,
        DateTimeOffset intervalStart,
        DateTimeOffset? intervalEnd,
        DateTimeOffset now)
    {
        if (viewportEnd <= viewportStart) return null;

        var end = intervalEnd ?? now;
        if (end <= viewportStart || intervalStart >= viewportEnd) return null;

        var clampedStart = intervalStart < viewportStart ? viewportStart : intervalStart;
        var clampedEnd = end > viewportEnd ? viewportEnd : end;

        var x = TimeToX(viewportStart, viewportEnd, width, clampedStart);
        var barWidth = TimeToX(viewportStart, viewportEnd, width, clampedEnd) - x;
        var fullSeconds = (end - intervalStart).TotalSeconds;

        return new BarSpan(x, barWidth, fullSeconds);
    }
}
