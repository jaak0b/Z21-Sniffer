using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public interface IIntervalChartDrawingStrategy
{
    double LaneHeight(double zoomFraction);

    void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context, ChartViewport viewport);
}
