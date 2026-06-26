using Z21Sniffer.Core.Model;

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

    public IReadOnlyList<TimelineBar> Bars(
        TimelineViewport viewport,
        IReadOnlyList<SensorKey> rows,
        IReadOnlyList<SensorInterval> intervals,
        DateTimeOffset now,
        double? highlightUnderSeconds = null,
        double verticalOffset = 0,
        double visibleHeight = double.PositiveInfinity)
    {
        var bars = new List<TimelineBar>();
        foreach (var interval in intervals)
        {
            var rowIndex = IndexOf(rows, interval.Sensor);
            if (rowIndex < 0) continue;

            var rowTop = rowIndex * viewport.RowHeight;
            if (rowTop + viewport.RowHeight <= verticalOffset || rowTop >= verticalOffset + visibleHeight) continue;

            var end = interval.End ?? now;
            if (end <= viewport.Start || interval.Start >= viewport.End) continue;

            var clampedStart = interval.Start < viewport.Start ? viewport.Start : interval.Start;
            var clampedEnd = end > viewport.End ? viewport.End : end;

            var x = TimeToX(viewport, clampedStart);
            var width = TimeToX(viewport, clampedEnd) - x;
            var fullSeconds = (end - interval.Start).TotalSeconds;
            var highlighted = highlightUnderSeconds is { } threshold && fullSeconds < threshold;

            bars.Add(new TimelineBar(
                interval.Sensor,
                rowIndex,
                x,
                rowTop,
                width,
                viewport.RowHeight,
                highlighted,
                fullSeconds));
        }

        return bars;
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

    private int IndexOf(IReadOnlyList<SensorKey> rows, SensorKey sensor)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] == sensor) return i;
        }

        return -1;
    }
}
