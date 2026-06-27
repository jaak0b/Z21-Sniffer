using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class TrackPowerIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    public object CreateContent(IIntervalSource source) =>
        new TrackPowerLegendContentViewModel((TrackPowerSource)source);
}
