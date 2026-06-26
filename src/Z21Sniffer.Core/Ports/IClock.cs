namespace Z21Sniffer.Core.Ports;

public interface IClock
{
    DateTimeOffset Now { get; }
}
