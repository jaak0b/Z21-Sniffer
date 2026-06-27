using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed class TrackPowerLegendContentViewModel
{
    public TrackPowerLegendContentViewModel(TrackPowerSource source) => Source = source;

    public TrackPowerSource Source { get; }

    public string Label => LocalizationService.Instance["TrackPower"];

    public string Details => LocalizationService.Instance["TrackPowerDetails"];
}
