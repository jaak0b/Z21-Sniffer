using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Tests;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;

    public void Advance(TimeSpan by) => Now += by;
}
