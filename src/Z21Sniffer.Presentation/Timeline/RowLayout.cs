using Autofac.Features.Indexed;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public readonly record struct RowBound(IIntervalSource Source, double Top, double Height);

public sealed class RowLayout
{
    private readonly IIndex<Type, IIntervalChartDrawingStrategy> _strategies;

    public RowLayout(IIndex<Type, IIntervalChartDrawingStrategy> strategies) => _strategies = strategies;

    public IReadOnlyList<RowBound> Compute(IReadOnlyList<IIntervalSource> sources, double zoomFraction)
    {
        var rows = new List<RowBound>();
        var top = 0.0;
        foreach (var source in sources.OrderBy(source => source.Order))
        {
            var height = _strategies[source.IntervalType].LaneHeight(zoomFraction);
            rows.Add(new RowBound(source, top, height));
            top += height;
        }

        return rows;
    }

    public IIntervalSource? SourceAt(IReadOnlyList<IIntervalSource> sources, double zoomFraction, double y)
    {
        foreach (var row in Compute(sources, zoomFraction))
        {
            if (y >= row.Top && y < row.Top + row.Height) return row.Source;
        }

        return null;
    }
}
