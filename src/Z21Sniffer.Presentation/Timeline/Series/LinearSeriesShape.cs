namespace Z21Sniffer.Presentation.Timeline.Series;

public sealed class LinearSeriesShape : ISeriesShape
{
    public double? ValueAt(IReadOnlyList<SeriesPoint> points, DateTimeOffset at)
    {
        if (points.Count == 0 || at < points[0].At) return null;

        for (var index = 1; index < points.Count; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            if (at > current.At) continue;

            var span = (current.At - previous.At).TotalSeconds;
            if (span <= 0) return current.Value;

            var fraction = (at - previous.At).TotalSeconds / span;
            return previous.Value + (current.Value - previous.Value) * fraction;
        }

        return points[^1].Value;
    }

    public IReadOnlyList<PlotPoint> BuildPath(IReadOnlyList<PlotPoint> corners) => corners;
}
