using Z21Sniffer.Core.Ports;
using Z21Sniffer.Infrastructure.Simulation;
using Z21Sniffer.Infrastructure.Z21;

namespace Z21Sniffer.Infrastructure;

public sealed class CommandStationConnectionFactory : ICommandStationConnectionFactory
{
    private readonly Z21CommandStationConnection _live;
    private readonly SimulatedCommandStationConnection _simulated;

    public CommandStationConnectionFactory(
        Z21CommandStationConnection live,
        SimulatedCommandStationConnection simulated)
    {
        _live = live;
        _simulated = simulated;
    }

    public ICommandStationConnection Create(bool simulated) => simulated ? _simulated : _live;
}
