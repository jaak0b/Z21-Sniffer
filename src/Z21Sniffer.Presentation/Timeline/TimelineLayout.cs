namespace Z21Sniffer.Presentation.Timeline;

public sealed class TimelineLayout
{
    private readonly BarGeometry _geometry = new();

    public double TimeToX(TimelineViewport viewport, DateTimeOffset time) =>
        _geometry.TimeToX(viewport.Start, viewport.End, viewport.Width, time);

    public DateTimeOffset XToTime(TimelineViewport viewport, double x) =>
        _geometry.XToTime(viewport.Start, viewport.End, viewport.Width, x);

    public bool RangesOverlap(double startA, double endA, double startB, double endB) =>
        startA < endB && startB < endA;

    public double CenteredLabelX(double cursorX, double labelWidth, double viewportWidth) =>
        Math.Clamp(cursorX - labelWidth / 2, 0, Math.Max(0, viewportWidth - labelWidth));

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
