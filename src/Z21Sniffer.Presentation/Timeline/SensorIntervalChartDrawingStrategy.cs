using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class SensorIntervalChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    private const double FlagWidth = 4;

    private readonly SensorBarText _barText = new();

    public double PreferredLaneHeight => 26;

    public void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context)
    {
        var sensorSource = (FeedbackSensorSource)source;
        var sensorInterval = (FeedbackSensorInterval)interval;

        surface.Fill(rect, new TimelineInk(context.Highlighted ? TimelineInkKeys.HighlightedBar : TimelineInkKeys.Bar));
        if (context.Highlighted) surface.Stroke(rect, new TimelineInk(TimelineInkKeys.HighlightOutline), 2);

        if (sensorInterval.EndReason == IntervalEndReason.Stopped)
        {
            var flagWidth = Math.Min(FlagWidth, rect.W);
            surface.Fill(rect with { X = rect.X + rect.W - flagWidth, W = flagWidth }, new TimelineInk(TimelineInkKeys.StoppedFlag));
        }

        if (!context.ShowContent) return;

        var text = _barText.Describe(sensorSource.Label, sensorInterval.Sensor, context.FullDuration);
        surface.Text(text, rect.X + 5, rect.Y + rect.H / 2, new TimelineInk(TimelineInkKeys.BarText));
    }
}
