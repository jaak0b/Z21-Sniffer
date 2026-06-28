using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline.Series;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class LocoIntervalChartDrawingStrategy : SampledSeriesChartDrawingStrategy
{
    private readonly SeriesHold _hold = new();

    protected override ISeriesShape Shape { get; } = new SteppedSeriesShape();

    protected override string BarInk => TimelineInkKeys.LocoBar;

    protected override string LineInk => TimelineInkKeys.LocoSpeedLine;

    protected override string BaselineInk => TimelineInkKeys.LocoBaseline;

    protected override string TextInk => TimelineInkKeys.LocoText;

    protected override SeriesPlot BuildPlot(IIntervalSource source, IInterval interval, ChartViewport viewport, BarRect rect)
    {
        var loco = (LocoInterval)interval;
        var baseline = BaselineFor(loco, rect);
        var points = loco.Samples
            .Select(sample => new PlotPoint(
                Geometry.TimeToX(viewport.Start, viewport.End, viewport.Width, sample.At),
                SpeedY(sample.Speed, sample.Forward, loco.MaxSpeed, baseline)))
            .ToList();

        return new SeriesPlot(baseline.Zero, points);
    }

    protected override string LabelFor(IIntervalSource source)
    {
        var loco = (LocoIntervalSource)source;
        var labelled = string.Create(CultureInfo.CurrentCulture, $"{LocalizationService.Instance["LocoPrefix"]} {loco.Address}");
        return loco.HasAlias
            ? string.Create(CultureInfo.CurrentCulture, $"{loco.Label} · {labelled}")
            : labelled;
    }

    public override string? Probe(IIntervalSource source, IInterval interval, DateTimeOffset at)
    {
        var loco = (LocoInterval)interval;
        var index = _hold.LastIndexAtOrBefore(loco.Samples, at, sample => sample.At);
        if (index < 0) return null;

        var current = loco.Samples[index];
        var speedWord = LocalizationService.Instance["SpeedLabel"];
        var direction = LocalizationService.Instance[current.Forward ? "LogForward" : "LogBackward"];
        return string.Create(CultureInfo.CurrentCulture, $"{speedWord} {current.Speed} · {direction}");
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

    private readonly record struct Baseline(double Zero, double Up, double Down);
}
