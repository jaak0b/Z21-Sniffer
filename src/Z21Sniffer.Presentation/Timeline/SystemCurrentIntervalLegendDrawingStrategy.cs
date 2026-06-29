using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class SystemCurrentIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    public string TypeLabel => LocalizationService.Instance["SystemCurrent"];

    public string IconGeometry => "M1,13 L4,13 L6,4 L9,15 L11,9 L13,9";

    public string RowLabel(IIntervalSource source) => LocalizationService.Instance["SystemCurrent"];

    public object CreateContent(IIntervalSource source) =>
        new SystemCurrentLegendContentViewModel((SystemCurrentSource)source);
}
