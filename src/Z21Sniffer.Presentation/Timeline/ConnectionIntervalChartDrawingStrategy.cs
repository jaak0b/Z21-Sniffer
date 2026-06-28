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

        surface.Text(Describe(connection.Connected, context.FullDuration), rect.X + 5, rect.Y + rect.H / 2, new TimelineInk(TimelineInkKeys.ConnectionText));
    }

    public string? Probe(IIntervalSource source, IInterval interval, DateTimeOffset at)
    {
        var connection = (ConnectionInterval)interval;
        return Describe(connection.Connected, at - interval.Start);
    }

    private string Describe(bool connected, TimeSpan duration)
    {
        var state = LocalizationService.Instance[connected ? "Connected" : "Disconnected"];
        var seconds = duration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        return $"{state} · {seconds} s";
    }
}
