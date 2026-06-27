using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class ConnectionIntervalChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    public double LaneHeight(double zoomFraction) => 26;

    public void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context, ChartViewport viewport)
    {
        var connection = (ConnectionInterval)interval;

        surface.Fill(rect, new TimelineInk(connection.Connected ? TimelineInkKeys.Connected : TimelineInkKeys.Disconnected));

        if (!context.ShowContent) return;

        var state = LocalizationService.Instance[connection.Connected ? "Connected" : "Disconnected"];
        var seconds = context.FullDuration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        surface.Text($"{state} · {seconds} s", rect.X + 5, rect.Y + rect.H / 2, new TimelineInk(TimelineInkKeys.ConnectionText));
    }
}
