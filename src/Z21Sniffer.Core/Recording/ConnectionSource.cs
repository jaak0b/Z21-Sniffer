using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class ConnectionSource : IntervalSourceBase<ConnectionInterval>
{
    public void Set(bool connected, DateTimeOffset at)
    {
        if (CurrentInterval is { } current && current.Connected == connected) return;

        CloseInterval(at, IntervalEndReason.FallingEdge);
        CreateInterval(at).Connected = connected;
    }
}
