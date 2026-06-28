using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed class SystemCurrentLegendContentViewModel
{
    public SystemCurrentLegendContentViewModel(SystemCurrentSource source) => Source = source;

    public SystemCurrentSource Source { get; }

    public string Label => LocalizationService.Instance["SystemCurrent"];

    public string Details => LocalizationService.Instance["SystemCurrentDetails"];
}
