namespace Z21Sniffer.Presentation.Timeline;

public sealed record TimelineWindow(DateTimeOffset End, TimeSpan Duration)
{
    public DateTimeOffset Start => End - Duration;
}

public sealed class TimelineViewportCalculator
{
    public TimeSpan MinDuration { get; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxDuration { get; } = TimeSpan.FromHours(1);

    public TimelineWindow Clamp(TimelineWindow window, DateTimeOffset earliest, DateTimeOffset now)
    {
        var duration = window.Duration < MinDuration
            ? MinDuration
            : (window.Duration > MaxDuration ? MaxDuration : window.Duration);

        var minEnd = earliest + duration;
        var end = window.End;
        if (end < minEnd) end = minEnd;
        if (end > now) end = now;

        return new TimelineWindow(end, duration);
    }

    public TimelineWindow Pan(TimelineWindow window, TimeSpan delta, DateTimeOffset earliest, DateTimeOffset now) =>
        Clamp(window with { End = window.End + delta }, earliest, now);

    public TimelineWindow Zoom(TimelineWindow window, double factor, double anchorFraction, DateTimeOffset earliest, DateTimeOffset now)
    {
        var anchorTime = window.Start + TimeSpan.FromSeconds(window.Duration.TotalSeconds * anchorFraction);
        var newDuration = TimeSpan.FromSeconds(window.Duration.TotalSeconds * factor);
        var newStart = anchorTime - TimeSpan.FromSeconds(newDuration.TotalSeconds * anchorFraction);
        return Clamp(new TimelineWindow(newStart + newDuration, newDuration), earliest, now);
    }

    public double TotalSeconds(DateTimeOffset earliest, DateTimeOffset now) =>
        Math.Max(0, (now - earliest).TotalSeconds);

    public double MaxScrollSeconds(TimelineWindow window, DateTimeOffset earliest, DateTimeOffset now) =>
        Math.Max(0, TotalSeconds(earliest, now) - window.Duration.TotalSeconds);

    public double ScrollSeconds(TimelineWindow window, DateTimeOffset earliest, DateTimeOffset now)
    {
        var offset = (window.Start - earliest).TotalSeconds;
        return Math.Clamp(offset, 0, MaxScrollSeconds(window, earliest, now));
    }

    public TimelineWindow WithStartOffset(TimelineWindow window, double offsetSeconds, DateTimeOffset earliest, DateTimeOffset now)
    {
        var start = earliest.AddSeconds(offsetSeconds);
        return Clamp(window with { End = start + window.Duration }, earliest, now);
    }
}
