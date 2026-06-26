using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class ConnectionIntervalChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    public double PreferredLaneHeight => 26;

    public void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context)
    {
        var connection = (ConnectionInterval)interval;

        surface.Fill(rect, new TimelineInk(connection.Connected ? TimelineInkKeys.Connected : TimelineInkKeys.Disconnected));

        if (!context.ShowContent) return;

        surface.Text(LocalizationService.Instance["Connection"], rect.X + 5, rect.Y + rect.H / 2, new TimelineInk(TimelineInkKeys.ConnectionText));
    }
}
