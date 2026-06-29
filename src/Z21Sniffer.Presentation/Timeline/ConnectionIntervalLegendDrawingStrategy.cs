using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class ConnectionIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    public string TypeLabel => LocalizationService.Instance["Connection"];

    public string IconGeometry => "M1,8 a2.4,2.4 0 1,0 4.8,0 a2.4,2.4 0 1,0 -4.8,0 Z M10.2,8 a2.4,2.4 0 1,0 4.8,0 a2.4,2.4 0 1,0 -4.8,0 Z M5,7.1 H11 V8.9 H5 Z";

    public string RowLabel(IIntervalSource source) => LocalizationService.Instance["Connection"];

    public object CreateContent(IIntervalSource source) =>
        new ConnectionLegendContentViewModel((ConnectionSource)source);
}
