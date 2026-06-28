namespace Z21Sniffer.Presentation.Timeline;

public sealed class TimelineLayout
{
    private readonly BarGeometry _geometry = new();

    public double TimeToX(TimelineViewport viewport, DateTimeOffset time) =>
        _geometry.TimeToX(viewport.Start, viewport.End, viewport.Width, time);

    public DateTimeOffset XToTime(TimelineViewport viewport, double x) =>
        _geometry.XToTime(viewport.Start, viewport.End, viewport.Width, x);

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
