using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Ports;

public interface IFeedbackSource
{
    event EventHandler<IReadOnlyList<SensorState>>? FeedbackReceived;
}
