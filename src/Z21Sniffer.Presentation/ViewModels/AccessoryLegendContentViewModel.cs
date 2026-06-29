using System.Globalization;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed class AccessoryLegendContentViewModel : AliasedLegendContentViewModel
{
    private readonly AccessorySource _source;

    public AccessoryLegendContentViewModel(AccessorySource source, IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
        : base(source, registry, confirmation) => _source = source;

    public override string Details => string.Format(CultureInfo.CurrentCulture, LocalizationService.Instance["AccessoryDetails"], _source.Address);
}
