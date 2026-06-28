using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class LocoIntervalChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    private const double BaseHeight = 34;
    private const double Inset = 3;
    private const double FlagWidth = 4;
    private const double LineThickness = 2;
    private const double MarkerRadius = 2.5;
    private const double MarkerThickness = 2.5;

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
        var baseline = BaselineFor(loco, rect);
        var plotted = loco.Samples
            .Select(sample => new PlottedSample(sample, _geometry.TimeToX(viewport.Start, viewport.End, viewport.Width, sample.At), SpeedY(sample.Speed, sample.Forward, loco.MaxSpeed, baseline)))
            .ToList();

        var onScreen = plotted.Where(sample => sample.X >= left && sample.X <= right).ToList();
        var entry = plotted.LastOrDefault(sample => sample.X < left);

        var corners = new List<PlotPoint>();
        if (entry is { } enter) corners.Add(new PlotPoint(left, enter.Y));
        corners.AddRange(onScreen.Select(sample => new PlotPoint(sample.X, sample.Y)));
        if (corners.Count > 0) corners.Add(corners[^1] with { X = right });
        surface.Polyline(Step(corners), new TimelineInk(TimelineInkKeys.LocoSpeedLine), LineThickness);

        foreach (var sample in onScreen)
            surface.Marker(sample.X, sample.Y, MarkerRadius, new TimelineInk(TimelineInkKeys.LocoSpeedLine), MarkerThickness);

        if (!context.ShowContent) return;

        var speedWord = LocalizationService.Instance["SpeedLabel"];
        foreach (var sample in onScreen)
        {
            var direction = LocalizationService.Instance[sample.Sample.Forward ? "LogForward" : "LogBackward"];
            var tooltip = string.Create(CultureInfo.CurrentCulture, $"{speedWord} {sample.Sample.Speed} · {direction} · {sample.Sample.At:HH:mm:ss}");
            surface.Hit(new BarRect(sample.X - 4, sample.Y - 4, 8, 8), tooltip);
        }

        surface.Text(Identity(locoSource), rect.X + 5, rect.Y + Inset + 6, new TimelineInk(TimelineInkKeys.LocoText));
    }

    private string Identity(LocoIntervalSource source)
    {
        var labelled = string.Create(CultureInfo.CurrentCulture, $"{LocalizationService.Instance["LocoPrefix"]} {source.Address}");
        return source.HasAlias
            ? string.Create(CultureInfo.CurrentCulture, $"{source.Label} · {labelled}")
            : labelled;
    }

    private Baseline BaselineFor(LocoInterval loco, BarRect rect)
    {
        var top = rect.Y + Inset;
        var bottom = rect.Y + rect.H - Inset;
        var usable = bottom - top;
        var hasForward = loco.Samples.Any(sample => sample.Forward);
        var hasReverse = loco.Samples.Any(sample => !sample.Forward);

        if (hasForward && hasReverse) return new Baseline(top + usable / 2, usable / 2, usable / 2);
        if (hasReverse) return new Baseline(top, 0, usable);
        return new Baseline(bottom, usable, 0);
    }

    private double SpeedY(int speed, bool forward, int maxSpeed, Baseline baseline)
    {
        var fraction = maxSpeed > 0 ? Math.Clamp((double)speed / maxSpeed, 0, 1) : 0;
        return forward ? baseline.Zero - fraction * baseline.Up : baseline.Zero + fraction * baseline.Down;
    }

    private List<PlotPoint> Step(IReadOnlyList<PlotPoint> corners)
    {
        if (corners.Count == 0) return new List<PlotPoint>();

        var stepped = new List<PlotPoint> { corners[0] };
        for (var index = 1; index < corners.Count; index++)
        {
            stepped.Add(new PlotPoint(corners[index].X, corners[index - 1].Y));
            stepped.Add(corners[index]);
        }

        return stepped;
    }

    private sealed record PlottedSample(LocoSpeedSample Sample, double X, double Y);

    private readonly record struct Baseline(double Zero, double Up, double Down);
}
