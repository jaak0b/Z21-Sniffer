using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Infrastructure.Simulation;

public sealed class SimulatedFeedbackScript
{
    private readonly SensorKey _arrival = new(1, 1);
    private readonly SensorKey _platform = new(1, 2);
    private readonly SensorKey _ghost = new(2, 3);

    public IReadOnlyList<SensorState> Frame(int tick) =>
    [
        new SensorState(_arrival, tick % 40 is >= 0 and < 20),
        new SensorState(_platform, tick % 40 is >= 8 and < 30),
        new SensorState(_ghost, tick % 7 == 0)
    ];

    public SystemSnapshot System(int tick) => new(
        MainCurrentMilliamps: 300 + tick % 50,
        SupplyVoltageMillivolts: 15000,
        TemperatureCelsius: 30 + tick % 5,
        ShortCircuit: tick % 60 == 0 && tick > 0,
        EmergencyStop: false,
        TrackVoltageOff: false,
        PowerLost: false,
        HighTemperature: false);

    public LocoSnapshot Loco(int tick) => new(Address: 3, Speed: tick % 128, Forward: tick % 24 < 12);

    public TurnoutSnapshot Turnout(int tick) => new(
        Address: 5,
        Position: tick % 36 < 18 ? TurnoutPosition.Output1 : TurnoutPosition.Output2);
}
