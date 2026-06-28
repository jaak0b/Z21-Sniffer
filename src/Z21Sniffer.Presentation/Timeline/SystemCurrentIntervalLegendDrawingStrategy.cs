using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Timeline;

public sealed class SystemCurrentIntervalLegendDrawingStrategy : IIntervalLegendDrawingStrategy
{
    public object CreateContent(IIntervalSource source) =>
        new SystemCurrentLegendContentViewModel((SystemCurrentSource)source);
}
