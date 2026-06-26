using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
