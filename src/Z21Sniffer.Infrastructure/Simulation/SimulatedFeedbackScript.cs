using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Infrastructure.Simulation;

public sealed class SimulatedFeedbackScript
{
    private const int MainModule = 1;
    private const int MainBlocks = 6;
    private const int GhostPeriod = 50;

    private readonly int[] _ghostBlips = { 7, 23, 24, 41 };

    private readonly IReadOnlyList<Train> _trains = new[]
    {
        new Train(Address: 3, MaxSpeed: 126, new Leg[]
        {
            new(Block: 1, Speed: 30, Forward: true, Ticks: 8),
            new(Block: 2, Speed: 60, Forward: true, Ticks: 8),
            new(Block: 3, Speed: 90, Forward: true, Ticks: 8),
            new(Block: 4, Speed: 120, Forward: true, Ticks: 8),
            new(Block: 4, Speed: 70, Forward: false, Ticks: 8),
            new(Block: 3, Speed: 90, Forward: false, Ticks: 8),
            new(Block: 2, Speed: 50, Forward: false, Ticks: 8),
            new(Block: 1, Speed: 0, Forward: true, Ticks: 8),
        }),
        new Train(Address: 7, MaxSpeed: 28, new Leg[]
        {
            new(Block: 5, Speed: 14, Forward: true, Ticks: 10),
            new(Block: 6, Speed: 24, Forward: true, Ticks: 10),
            new(Block: 6, Speed: 0, Forward: true, Ticks: 6),
            new(Block: 5, Speed: 18, Forward: false, Ticks: 10),
        }),
    };

    public IReadOnlyList<SensorState> Frame(int tick)
    {
        var states = new List<SensorState>();
        for (var block = 1; block <= MainBlocks; block++)
        {
            var occupied = _trains.Any(train => CurrentLeg(train, tick).Block == block);
            states.Add(new SensorState(new SensorKey(MainModule, block), occupied));
        }

        states.Add(new SensorState(new SensorKey(2, 3), _ghostBlips.Contains(tick % GhostPeriod)));
        return states;
    }

    public IReadOnlyList<LocoSnapshot> Locos(int tick) =>
        _trains.Select(train => Snapshot(train, CurrentLeg(train, tick))).ToList();

    public SystemSnapshot System(int tick) => new(
        MainCurrentMilliamps: 300 + tick % 50,
        SupplyVoltageMillivolts: 15000,
        TemperatureCelsius: 30 + tick % 5,
        ShortCircuit: tick % 60 == 0 && tick > 0,
        EmergencyStop: false,
        TrackVoltageOff: tick % 80 is >= 40 and < 55,
        ProgrammingMode: tick % 80 is >= 20 and < 30,
        PowerLost: false,
        HighTemperature: false);

    public TurnoutSnapshot Turnout(int tick) => new(
        Address: 5,
        Position: tick % 36 < 18 ? TurnoutPosition.Output1 : TurnoutPosition.Output2);

    public StationHardware Hardware() => new(TypeCode: 513, FirmwareVersion: 0x0140);

    private LocoSnapshot Snapshot(Train train, Leg leg) => new(train.Address, leg.Speed, leg.Forward, train.MaxSpeed);

    private Leg CurrentLeg(Train train, int tick)
    {
        var cycle = train.Route.Sum(leg => leg.Ticks);
        var phase = ((tick % cycle) + cycle) % cycle;
        foreach (var leg in train.Route)
        {
            if (phase < leg.Ticks) return leg;
            phase -= leg.Ticks;
        }

        return train.Route[^1];
    }

    private sealed record Leg(int Block, int Speed, bool Forward, int Ticks);

    private sealed record Train(int Address, int MaxSpeed, IReadOnlyList<Leg> Route);
}
