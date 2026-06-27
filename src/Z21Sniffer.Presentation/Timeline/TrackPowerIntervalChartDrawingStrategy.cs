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

        var name = LocalizationService.Instance[NameKeyFor(power.Status)];
        var seconds = context.FullDuration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        surface.Text($"{name} · {seconds} s", rect.X + 5, rect.Y + rect.H / 2, new TimelineInk(TextFor(power.Status)));
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
