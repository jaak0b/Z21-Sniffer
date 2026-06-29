using System.Globalization;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed class SensorLegendContentViewModel : AliasedLegendContentViewModel
{
    private readonly FeedbackSensorSource _source;

    public SensorLegendContentViewModel(FeedbackSensorSource source, IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
        : base(source, registry, confirmation) => _source = source;

    public override string Details => string.Format(CultureInfo.CurrentCulture, LocalizationService.Instance["SensorDetails"], _source.Sensor.Module, _source.Sensor.Contact);
}
