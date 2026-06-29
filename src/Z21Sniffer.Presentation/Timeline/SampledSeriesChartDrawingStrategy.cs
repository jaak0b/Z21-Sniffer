using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline.Series;

namespace Z21Sniffer.Presentation.Timeline;

public readonly record struct SeriesPlot(double BaselineY, IReadOnlyList<PlotPoint> Points);

public abstract class SampledSeriesChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    protected const double BaseHeight = 34;
    protected const double Inset = 3;
    protected const double FlagWidth = 4;
    protected const double LineThickness = 2;
    protected const double BaselineThickness = 1;
    protected const double MaxMarkerSize = 2.5;

    protected BarGeometry Geometry { get; } = new();

    protected abstract ISeriesShape Shape { get; }

    protected abstract string BarInk { get; }

    protected abstract string LineInk { get; }

    protected abstract string BaselineInk { get; }

    protected abstract string TextInk { get; }

    public double LaneHeight(double zoomFraction) => BaseHeight * (1 + Math.Clamp(zoomFraction, 0, 1));

    public void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context, ChartViewport viewport)
    {
        surface.Fill(rect, new TimelineInk(BarInk));

        if (interval.EndReason == IntervalEndReason.Stopped)
        {
            var flagWidth = Math.Min(FlagWidth, rect.W);
            surface.Fill(rect with { X = rect.X + rect.W - flagWidth, W = flagWidth }, new TimelineInk(TimelineInkKeys.StoppedFlag));
        }

        var plot = BuildPlot(source, interval, viewport, rect);
        var left = rect.X;
        var right = rect.X + rect.W;
        surface.Line(left, plot.BaselineY, right, plot.BaselineY, new TimelineInk(BaselineInk), BaselineThickness, dashed: true);

        var onScreen = plot.Points.Where(point => point.X >= left && point.X <= right).ToList();
        var entry = EntryBefore(plot.Points, left);

        var corners = new List<PlotPoint>();
        if (entry is { } enter) corners.Add(new PlotPoint(left, enter.Y));
        corners.AddRange(onScreen);
        if (corners.Count > 0) corners.Add(corners[^1] with { X = right });
        surface.Polyline(Shape.BuildPath(corners), new TimelineInk(LineInk), LineThickness);

        var markerSize = MarkerSizeFor(rect.H);
        foreach (var point in onScreen)
            surface.Marker(point.X, point.Y, markerSize, new TimelineInk(LineInk), markerSize);

        if (!context.ShowContent) return;

        surface.Text(LabelFor(source), rect.X + 5, rect.Y + Inset + 6, new TimelineInk(TextInk));
    }

    protected double MarkerSizeFor(double laneHeight)
    {
        var zoom = Math.Clamp(laneHeight / BaseHeight - 1, 0, 1);
        return MaxMarkerSize * zoom * zoom;
    }

    public abstract string? Probe(IIntervalSource source, IInterval interval, DateTimeOffset at);

    protected abstract SeriesPlot BuildPlot(IIntervalSource source, IInterval interval, ChartViewport viewport, BarRect rect);

    protected abstract string LabelFor(IIntervalSource source);

    private static PlotPoint? EntryBefore(IReadOnlyList<PlotPoint> points, double left)
    {
        PlotPoint? entry = null;
        foreach (var point in points)
        {
            if (point.X < left) entry = point;
        }

        return entry;
    }
}
