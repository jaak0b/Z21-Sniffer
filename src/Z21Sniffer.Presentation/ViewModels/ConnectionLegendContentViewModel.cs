using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed class ConnectionLegendContentViewModel
{
    public ConnectionLegendContentViewModel(ConnectionSource source) => Source = source;

    public ConnectionSource Source { get; }

    public string Label => LocalizationService.Instance["Connection"];
}
