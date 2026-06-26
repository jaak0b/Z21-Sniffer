namespace Z21Sniffer.Presentation.Timeline;

public sealed class TimelineLayout
{
    public double TimeToX(TimelineViewport viewport, DateTimeOffset time)
    {
        var span = (viewport.End - viewport.Start).TotalSeconds;
        if (span <= 0) return 0;
        var offset = (time - viewport.Start).TotalSeconds;
        return offset / span * viewport.Width;
    }

    public IReadOnlyList<TimelineTick> Ticks(TimelineViewport viewport, TimeSpan step)
    {
        var ticks = new List<TimelineTick>();
        if (step <= TimeSpan.Zero) return ticks;

        for (var time = viewport.Start; time <= viewport.End; time += step)
        {
            ticks.Add(new TimelineTick(time, TimeToX(viewport, time)));
        }

        return ticks;
    }
}
