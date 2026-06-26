namespace Z21Sniffer.Core.Ports;

public interface ICommandStationConnectionFactory
{
    ICommandStationConnection Create(bool simulated);
}
