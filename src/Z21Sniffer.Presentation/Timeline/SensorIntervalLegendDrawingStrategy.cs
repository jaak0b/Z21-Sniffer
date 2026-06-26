using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class SensorIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    private readonly IIntervalSourceRegistry _registry;
    private readonly IRemovalConfirmation _confirmation;

    public SensorIntervalLegendDrawingStrategy(IIntervalSourceRegistry registry, IRemovalConfirmation confirmation)
    {
        _registry = registry;
        _confirmation = confirmation;
    }

    public object CreateContent(IIntervalSource source) =>
        new SensorLegendContentViewModel((FeedbackSensorSource)source, _registry, _confirmation);
}
