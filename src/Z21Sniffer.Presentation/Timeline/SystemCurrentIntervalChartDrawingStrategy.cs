using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline.Series;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class SystemCurrentIntervalChartDrawingStrategy : SampledSeriesChartDrawingStrategy
{
    protected override ISeriesShape Shape { get; } = new LinearSeriesShape();

    protected override string BarInk => TimelineInkKeys.SystemCurrentBar;

    protected override string LineInk => TimelineInkKeys.SystemCurrentLine;

    protected override string BaselineInk => TimelineInkKeys.SystemCurrentBaseline;

    protected override string TextInk => TimelineInkKeys.SystemCurrentText;

    protected override SeriesPlot BuildPlot(IIntervalSource source, IInterval interval, ChartViewport viewport, BarRect rect)
    {
        var current = (SystemCurrentInterval)interval;
        var top = rect.Y + Inset;
        var bottom = rect.Y + rect.H - Inset;
        var scaleMax = ScaleMaxFor(current);
        var points = current.Samples
            .Select(sample => new PlotPoint(
                Geometry.TimeToX(viewport.Start, viewport.End, viewport.Width, sample.At),
                CurrentY(sample.Milliamps, scaleMax, top, bottom)))
            .ToList();

        return new SeriesPlot(bottom, points);
    }

    protected override string LabelFor(IIntervalSource source) => LocalizationService.Instance["SystemCurrent"];

    public override string? Probe(IIntervalSource source, IInterval interval, DateTimeOffset at)
    {
        var current = (SystemCurrentInterval)interval;
        var points = current.Samples.Select(sample => new SeriesPoint(sample.At, sample.Milliamps)).ToList();
        if (Shape.ValueAt(points, at) is not { } value) return null;

        var milliamps = (int)Math.Round(value);
        return current is { DeviceName: { } name, MaxCurrentMilliamps: { } max }
            ? string.Format(CultureInfo.CurrentCulture, LocalizationService.Instance["SystemCurrentReadingNamed"], name, milliamps, max)
            : string.Format(CultureInfo.CurrentCulture, LocalizationService.Instance["SystemCurrentReading"], milliamps);
    }

    private int ScaleMaxFor(SystemCurrentInterval interval)
    {
        if (interval.MaxCurrentMilliamps is { } known) return known;
        var peak = interval.Samples.Count == 0 ? 0 : interval.Samples.Max(sample => sample.Milliamps);
        return Math.Max(1, peak);
    }

    private double CurrentY(int milliamps, int scaleMax, double top, double bottom)
    {
        var fraction = scaleMax > 0 ? Math.Clamp((double)milliamps / scaleMax, 0, 1) : 0;
        return bottom - fraction * (bottom - top);
    }
}
