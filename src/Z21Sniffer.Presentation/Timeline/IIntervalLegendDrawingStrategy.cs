using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.Timeline;

public interface IIntervalLegendDrawingStrategy
{
    object CreateContent(IIntervalSource source);
}
