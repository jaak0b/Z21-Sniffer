using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class LocoIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    private readonly IIntervalSourceRegistry _registry;
    private readonly IRemovalConfirmation _confirmation;

    public LocoIntervalLegendDrawingStrategy(IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
    {
        _registry = registry;
        _confirmation = confirmation;
    }

    public string TypeLabel => LocalizationService.Instance["TypeLoco"];

    public string IconGeometry => "M2,5 H11 L14,8 V11 H2 Z M3.4,12 a1.6,1.6 0 1,0 3.2,0 a1.6,1.6 0 1,0 -3.2,0 Z M9.4,12 a1.6,1.6 0 1,0 3.2,0 a1.6,1.6 0 1,0 -3.2,0 Z";

    public bool IconStroked => false;

    public string RowLabel(IIntervalSource source) => ((LocoIntervalSource)source).Label;

    public object CreateContent(IIntervalSource source) =>
        new LocoLegendContentViewModel((LocoIntervalSource)source, _registry, _confirmation);
}
