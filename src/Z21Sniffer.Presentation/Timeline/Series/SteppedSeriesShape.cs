namespace Z21Sniffer.Presentation.Timeline.Series;

public sealed class SteppedSeriesShape : ISeriesShape
{
    private readonly SeriesHold _hold = new();

    public double? ValueAt(IReadOnlyList<SeriesPoint> points, DateTimeOffset at)
    {
        var index = _hold.LastIndexAtOrBefore(points, at, point => point.At);
        return index < 0 ? null : points[index].Value;
    }

    public IReadOnlyList<PlotPoint> BuildPath(IReadOnlyList<PlotPoint> corners)
    {
        if (corners.Count == 0) return corners;

        var stepped = new List<PlotPoint> { corners[0] };
        for (var index = 1; index < corners.Count; index++)
        {
            stepped.Add(new PlotPoint(corners[index].X, corners[index - 1].Y));
            stepped.Add(corners[index]);
        }

        return stepped;
    }
}
