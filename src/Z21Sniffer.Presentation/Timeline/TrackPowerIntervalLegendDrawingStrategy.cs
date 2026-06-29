using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class TrackPowerIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    public string TypeLabel => LocalizationService.Instance["TrackPower"];

    public string IconGeometry => "M9.5,1 L3,9 H7.2 L6.2,15 L13,6.5 H8.6 Z";

    public string RowLabel(IIntervalSource source) => LocalizationService.Instance["TrackPower"];

    public object CreateContent(IIntervalSource source) =>
        new TrackPowerLegendContentViewModel((TrackPowerSource)source);
}
