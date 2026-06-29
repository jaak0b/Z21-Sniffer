using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class AccessoryIntervalChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    private readonly AccessoryBarText _barText = new();

    public double LaneHeight(double zoomFraction) => 26;

    public void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context, ChartViewport viewport)
    {
        var accessorySource = (AccessorySource)source;
        var accessoryInterval = (AccessoryInterval)interval;

        surface.Fill(rect, new TimelineInk(FillFor(accessoryInterval.Position)));

        if (!context.ShowContent) return;

        var text = _barText.Describe(accessorySource.Label, accessoryInterval.Address, accessoryInterval.Position, context.FullDuration);
        surface.Text(text, rect.X + 5, rect.Y + rect.H / 2, new TimelineInk(TimelineInkKeys.AccessoryText));
    }

    public string? Probe(IIntervalSource source, IInterval interval, DateTimeOffset at)
    {
        var accessorySource = (AccessorySource)source;
        var accessoryInterval = (AccessoryInterval)interval;
        return _barText.Describe(accessorySource.Label, accessoryInterval.Address, accessoryInterval.Position, at - interval.Start);
    }

    private string FillFor(TurnoutPosition position) =>
        position == TurnoutPosition.Output2 ? TimelineInkKeys.AccessoryOutput2 : TimelineInkKeys.AccessoryOutput1;
}
