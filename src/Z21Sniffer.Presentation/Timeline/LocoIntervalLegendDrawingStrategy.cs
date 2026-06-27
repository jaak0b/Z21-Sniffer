using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
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

    public object CreateContent(IIntervalSource source) =>
        new LocoLegendContentViewModel((LocoIntervalSource)source, _registry, _confirmation);
}
