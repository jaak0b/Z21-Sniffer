using System.Globalization;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed class LocoLegendContentViewModel : AliasedLegendContentViewModel
{
    private readonly LocoIntervalSource _source;

    public LocoLegendContentViewModel(LocoIntervalSource source, IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
        : base(source, registry, confirmation) => _source = source;

    public override string Details => string.Format(CultureInfo.CurrentCulture, LocalizationService.Instance["LocoDetails"], _source.Address);
}
