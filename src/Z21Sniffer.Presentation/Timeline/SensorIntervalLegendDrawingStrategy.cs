using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
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

    public string TypeLabel => LocalizationService.Instance["TypeSensor"];

    public string IconGeometry => "M5,5 a3,3 0 1,0 6,0 a3,3 0 1,0 -6,0 Z M7.3,8 H8.7 V12 H7.3 Z M2,12 H14 V13.4 H2 Z";

    public bool IconStroked => false;

    public string RowLabel(IIntervalSource source) => ((FeedbackSensorSource)source).Label;

    public object CreateContent(IIntervalSource source) =>
        new SensorLegendContentViewModel((FeedbackSensorSource)source, _registry, _confirmation);
}
