using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class AccessoryIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    private readonly IIntervalSourceRegistry _registry;
    private readonly IRemovalConfirmation _confirmation;

    public AccessoryIntervalLegendDrawingStrategy(IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
    {
        _registry = registry;
        _confirmation = confirmation;
    }

    public string TypeLabel => LocalizationService.Instance["TypeAccessory"];

    public string IconGeometry => "M7.2,2 H8.8 V15 H7.2 Z M7.41,8.96 L8.59,10.04 L14.09,4.04 L12.91,2.96 Z";

    public string RowLabel(IIntervalSource source) => ((AccessorySource)source).Label;

    public object CreateContent(IIntervalSource source) =>
        new AccessoryLegendContentViewModel((AccessorySource)source, _registry, _confirmation);
}
