using Autofac.Features.Indexed;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class BarChartRenderer
{
    private readonly IIndex<Type, IIntervalChartDrawingStrategy> _strategies;
    private readonly RowLayout _rowLayout;
    private readonly BarGeometry _geometry = new();

    public BarChartRenderer(IIndex<Type, IIntervalChartDrawingStrategy> strategies)
    {
        _strategies = strategies;
        _rowLayout = new RowLayout(strategies);
    }

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
        foreach (var row in _rowLayout.Compute(sources, zoomFraction))
        {
            if (row.Top + row.Height <= verticalOffset || row.Top >= verticalOffset + visibleHeight) continue;

            var strategy = _strategies[row.Source.IntervalType];
            foreach (var interval in row.Source.Intervals)
            {
                if (_geometry.Compute(viewport.Start, viewport.End, viewport.Width, interval.Start, interval.End, now) is not { } span) continue;

                var rect = new BarRect(span.X, row.Top - verticalOffset, span.Width, row.Height);
                var highlighted = highlightUnderSeconds is { } threshold && span.FullDurationSeconds < threshold;
                var context = new BarContentContext(span.Width >= minContentWidth, highlighted, TimeSpan.FromSeconds(span.FullDurationSeconds));
                strategy.Draw(row.Source, interval, surface, rect, context, viewport);
            }
        }
    }
}
