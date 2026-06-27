using Autofac.Features.Indexed;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class BarChartRenderer
{
    private readonly IIndex<Type, IIntervalChartDrawingStrategy> _strategies;
    private readonly BarGeometry _geometry = new();

    public BarChartRenderer(IIndex<Type, IIntervalChartDrawingStrategy> strategies) => _strategies = strategies;

    public void Render(
        ITimelineSurface surface,
        IReadOnlyList<IIntervalSource> sources,
        ChartViewport viewport,
        DateTimeOffset now,
        double? highlightUnderSeconds,
        double verticalOffset,
        double visibleHeight,
        double minContentWidth,
        double zoomFraction)
    {
        var laneTop = 0.0;
        foreach (var source in sources.OrderBy(source => source.Order))
        {
            var strategy = _strategies[source.IntervalType];
            var laneHeight = strategy.LaneHeight(zoomFraction);

            if (laneTop + laneHeight > verticalOffset && laneTop < verticalOffset + visibleHeight)
            {
                foreach (var interval in source.Intervals)
                {
                    if (_geometry.Compute(viewport.Start, viewport.End, viewport.Width, interval.Start, interval.End, now) is not { } span) continue;

                    var rect = new BarRect(span.X, laneTop - verticalOffset, span.Width, laneHeight);
                    var highlighted = highlightUnderSeconds is { } threshold && span.FullDurationSeconds < threshold;
                    var context = new BarContentContext(span.Width >= minContentWidth, highlighted, TimeSpan.FromSeconds(span.FullDurationSeconds));
                    strategy.Draw(source, interval, surface, rect, context, viewport);
                }
            }

            laneTop += laneHeight;
        }
    }
}
