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
        double zoomFraction,
        double? highlightOverSeconds = null)
    {
        foreach (var row in _rowLayout.Compute(sources, zoomFraction))
        {
            if (row.Top + row.Height <= verticalOffset || row.Top >= verticalOffset + visibleHeight) continue;

            var strategy = _strategies[row.Source.IntervalType];
            var intervals = row.Source.Intervals;
            for (var index = 0; index < intervals.Count; index++)
            {
                var interval = intervals[index];
                if (_geometry.Compute(viewport.Start, viewport.End, viewport.Width, interval.Start, interval.End, now) is not { } span) continue;

                var squareLeft = index > 0 && intervals[index - 1].End == interval.Start;
                var squareRight = index + 1 < intervals.Count && intervals[index + 1].Start == interval.End;
                var rect = new BarRect(span.X, row.Top - verticalOffset, span.Width, row.Height, new BarCorners(squareLeft, squareRight));
                var context = new BarContentContext(span.Width >= minContentWidth, TimeSpan.FromSeconds(span.FullDurationSeconds));
                strategy.Draw(row.Source, interval, surface, rect, context, viewport);

                var withinUpperBound = highlightUnderSeconds is { } upper && span.FullDurationSeconds < upper;
                var withinLowerBound = highlightOverSeconds is not { } lower || span.FullDurationSeconds > lower;
                var highlighted = !interval.IsOpen
                    && row.Source.HighlightsShortIntervals
                    && withinUpperBound
                    && withinLowerBound;
                if (highlighted) surface.Stroke(rect, new TimelineInk(TimelineInkKeys.HighlightOutline), 2);
            }
        }
    }
}
