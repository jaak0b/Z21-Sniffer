using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class TrackPowerIntervalChartDrawingStrategy : IIntervalChartDrawingStrategy
{
    public double LaneHeight(double zoomFraction) => 26;

    public void Draw(IIntervalSource source, IInterval interval, ITimelineSurface surface, BarRect rect, BarContentContext context, ChartViewport viewport)
    {
        var power = (TrackPowerInterval)interval;

        surface.Fill(rect, new TimelineInk(FillFor(power.Status)));

        if (!context.ShowContent) return;

        surface.Text(Describe(power.Status, context.FullDuration), rect.X + 5, rect.Y + rect.H / 2, new TimelineInk(TextFor(power.Status)));
    }

    public string? Probe(IIntervalSource source, IInterval interval, DateTimeOffset at)
    {
        var power = (TrackPowerInterval)interval;
        return Describe(power.Status, at - interval.Start);
    }

    private string Describe(TrackPowerStatus status, TimeSpan duration)
    {
        var name = LocalizationService.Instance[NameKeyFor(status)];
        var seconds = duration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        return $"{name} · {seconds} s";
    }

    private string FillFor(TrackPowerStatus status) => status switch
    {
        TrackPowerStatus.On => TimelineInkKeys.TrackPowerOn,
        TrackPowerStatus.Short => TimelineInkKeys.TrackPowerShort,
        TrackPowerStatus.Programming => TimelineInkKeys.TrackPowerProgramming,
        _ => TimelineInkKeys.TrackPowerOff,
    };

    private string TextFor(TrackPowerStatus status) =>
        status == TrackPowerStatus.Off ? TimelineInkKeys.TrackPowerOffText : TimelineInkKeys.TrackPowerText;

    private string NameKeyFor(TrackPowerStatus status) => status switch
    {
        TrackPowerStatus.On => "TrackPowerOnState",
        TrackPowerStatus.Short => "TrackPowerShortState",
        TrackPowerStatus.Programming => "TrackPowerProgrammingState",
        _ => "TrackPowerOffState",
    };
}
