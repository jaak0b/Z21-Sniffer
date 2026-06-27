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

        var left = rect.X;
        var right = rect.X + rect.W;
        var plotted = loco.Samples
            .Select(sample => new PlottedSample(sample, _geometry.TimeToX(viewport.Start, viewport.End, viewport.Width, sample.At), SpeedY(sample.Speed, loco, rect)))
            .ToList();

        var onScreen = plotted.Where(sample => sample.X >= left && sample.X <= right).ToList();
        var entry = plotted.LastOrDefault(sample => sample.X < left);

        var points = new List<PlotPoint>();
        if (entry is { } enter) points.Add(new PlotPoint(left, enter.Y));
        points.AddRange(onScreen.Select(sample => new PlotPoint(sample.X, sample.Y)));
        if (points.Count > 0) points.Add(points[^1] with { X = right });
        surface.Polyline(points, new TimelineInk(TimelineInkKeys.LocoSpeedLine), 2);

        if (!context.ShowContent) return;

        var direction = LocalizationService.Instance[loco.Forward ? "LogForward" : "LogBackward"];
        foreach (var sample in onScreen)
        {
            var tooltip = string.Create(CultureInfo.CurrentCulture, $"{sample.Sample.Speed} · {direction} · {sample.Sample.At:HH:mm:ss}");
            surface.Hit(new BarRect(sample.X - 4, sample.Y - 4, 8, 8), tooltip);
        }

        var latest = loco.Samples.Select(sample => sample.Speed).LastOrDefault();
        var label = string.Create(CultureInfo.CurrentCulture, $"{locoSource.Label} · {latest}");
        surface.Text(label, rect.X + 5, rect.Y + Inset + 6, new TimelineInk(TimelineInkKeys.LocoText));
    }

    private double SpeedY(int speed, LocoInterval loco, BarRect rect)
    {
        var top = rect.Y + Inset;
        var bottom = rect.Y + rect.H - Inset;
        var usable = bottom - top;
        var fraction = loco.MaxSpeed > 0 ? Math.Clamp((double)speed / loco.MaxSpeed, 0, 1) : 0;
        return loco.Forward ? bottom - fraction * usable : top + fraction * usable;
    }

    private sealed record PlottedSample(LocoSpeedSample Sample, double X, double Y);
}
