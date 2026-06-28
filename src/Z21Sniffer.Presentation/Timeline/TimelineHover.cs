using Autofac.Features.Indexed;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class TimelineHover
{
    private readonly IIndex<Type, IIntervalChartDrawingStrategy> _strategies;
    private readonly RowLayout _rowLayout;
    private readonly BarGeometry _geometry = new();

    public TimelineHover(IIndex<Type, IIntervalChartDrawingStrategy> strategies)
    {
        _strategies = strategies;
        _rowLayout = new RowLayout(strategies);
    }

    public string? ValueAt(IReadOnlyList<IIntervalSource> sources, ChartViewport viewport, double zoomFraction, double x, double y)
    {
        if (_rowLayout.SourceAt(sources, zoomFraction, y) is not { } source) return null;

        var at = _geometry.XToTime(viewport.Start, viewport.End, viewport.Width, x);
        foreach (var interval in source.Intervals)
        {
            if (Covers(interval, at, viewport.End))
                return _strategies[source.IntervalType].Probe(source, interval, at);
        }

        return null;
    }

    private static bool Covers(IInterval interval, DateTimeOffset at, DateTimeOffset liveEdge) =>
        at >= interval.Start && at <= (interval.End ?? liveEdge);
}
