using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class SystemCurrentIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    public string TypeLabel => LocalizationService.Instance["SystemCurrent"];

    public string IconGeometry => "M1,11 L4,11 L6,4 L9,12 L11,8 L15,8";

    public bool IconStroked => true;

    public string RowLabel(IIntervalSource source) => LocalizationService.Instance["SystemCurrent"];

    public object CreateContent(IIntervalSource source) =>
        new SystemCurrentLegendContentViewModel((SystemCurrentSource)source);
}
