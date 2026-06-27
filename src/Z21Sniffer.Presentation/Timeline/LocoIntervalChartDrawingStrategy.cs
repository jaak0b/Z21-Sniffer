using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class LocoIntervalChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    private const double BaseHeight = 26;
    private const double Inset = 3;
    private const double FlagWidth = 4;

    private readonly BarGeometry _geometry = new();

    public double LaneHeight(double zoomFraction) => BaseHeight * (1 + Math.Clamp(zoomFraction, 0, 1));

    public void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context, ChartViewport viewport)
    {
        var locoSource = (LocoIntervalSource)source;
        var loco = (LocoInterval)interval;

        surface.Fill(rect, new TimelineInk(TimelineInkKeys.LocoBar));

        if (loco.EndReason == IntervalEndReason.Stopped)
        {
            var flagWidth = Math.Min(FlagWidth, rect.W);
            surface.Fill(rect with { X = rect.X + rect.W - flagWidth, W = flagWidth }, new TimelineInk(TimelineInkKeys.StoppedFlag));
        }

        var points = loco.Samples.Select(sample => Plot(sample, loco, rect, viewport)).ToList();
        if (points.Count > 0) points.Add(points[^1] with { X = rect.X + rect.W });
        surface.Polyline(points, new TimelineInk(TimelineInkKeys.LocoSpeedLine), 2);

        if (!context.ShowContent) return;

        var direction = LocalizationService.Instance[loco.Forward ? "LogForward" : "LogBackward"];
        for (var index = 0; index < loco.Samples.Count; index++)
        {
            var sample = loco.Samples[index];
            var point = points[index];
            var tooltip = string.Create(CultureInfo.CurrentCulture, $"{sample.Speed} · {direction} · {sample.At:HH:mm:ss}");
            surface.Hit(new BarRect(point.X - 4, point.Y - 4, 8, 8), tooltip);
        }

        var latest = loco.Samples.Select(sample => sample.Speed).LastOrDefault();
        var label = string.Create(CultureInfo.CurrentCulture, $"{locoSource.Label} · {latest}");
        surface.Text(label, rect.X + 5, rect.Y + Inset + 6, new TimelineInk(TimelineInkKeys.LocoText));
    }

    private PlotPoint Plot(LocoSpeedSample sample, LocoInterval loco, BarRect rect, ChartViewport viewport)
    {
        var rawX = _geometry.TimeToX(viewport.Start, viewport.End, viewport.Width, sample.At);
        var x = Math.Clamp(rawX, rect.X, rect.X + rect.W);

        var top = rect.Y + Inset;
        var bottom = rect.Y + rect.H - Inset;
        var usable = bottom - top;
        var fraction = loco.MaxSpeed > 0 ? Math.Clamp((double)sample.Speed / loco.MaxSpeed, 0, 1) : 0;
        var y = loco.Forward ? bottom - fraction * usable : top + fraction * usable;

        return new PlotPoint(x, y);
    }
}
