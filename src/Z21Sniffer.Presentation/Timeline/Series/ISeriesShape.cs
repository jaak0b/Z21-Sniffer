namespace Z21Sniffer.Presentation.Timeline.Series;

public interface ISeriesShape
{
    double? ValueAt(IReadOnlyList<SeriesPoint> points, DateTimeOffset at);

    IReadOnlyList<PlotPoint> BuildPath(IReadOnlyList<PlotPoint> corners);
}
