using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Infrastructure.Simulation;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class SimulatedFeedbackScriptTest
{
    private readonly SimulatedFeedbackScript _script = new();
    private static readonly SensorKey Ghost = new(2, 3);

    private bool Occupied(int tick, SensorKey sensor) => _script.Frame(tick).Single(s => s.Sensor == sensor).Occupied;

    private LocoSnapshot Loco(int tick, int address) => _script.Locos(tick).Single(l => l.Address == address);

    [Test]
    public void Frame_ReportsEverySensorEveryTick()
    {
        Assert.That(_script.Frame(0), Has.Count.EqualTo(7));
        Assert.That(_script.Frame(123), Has.Count.EqualTo(7));
    }

    [Test]
    public void Frame_TrainOccupiesConsecutiveBlocksAsItMoves()
    {
        Assert.That(Occupied(0, new SensorKey(1, 1)), Is.True);
        Assert.That(Occupied(8, new SensorKey(1, 1)), Is.False);
        Assert.That(Occupied(8, new SensorKey(1, 2)), Is.True);
        Assert.That(Occupied(16, new SensorKey(1, 3)), Is.True);
        Assert.That(Occupied(24, new SensorKey(1, 4)), Is.True);
    }

    [Test]
    public void Frame_OnlyTheCurrentBlockOfATrainIsOccupied()
    {
        Assert.That(Occupied(0, new SensorKey(1, 1)), Is.True);
        Assert.That(Occupied(0, new SensorKey(1, 2)), Is.False);
        Assert.That(Occupied(0, new SensorKey(1, 3)), Is.False);
        Assert.That(Occupied(0, new SensorKey(1, 4)), Is.False);
    }

    [Test]
    public void Frame_SecondTrainRunsOnItsOwnBlocksConcurrently()
    {
        Assert.That(Occupied(0, new SensorKey(1, 5)), Is.True);
        Assert.That(Occupied(10, new SensorKey(1, 6)), Is.True);
        Assert.That(Occupied(10, new SensorKey(1, 5)), Is.False);
    }

    [Test]
    public void Frame_GhostSensor_BlipsRarelyAndUnevenly()
    {
        Assert.That(Occupied(7, Ghost), Is.True);
        Assert.That(Occupied(23, Ghost), Is.True);
        Assert.That(Occupied(24, Ghost), Is.True);
        Assert.That(Occupied(41, Ghost), Is.True);
        Assert.That(Occupied(0, Ghost), Is.False);
        Assert.That(Occupied(8, Ghost), Is.False);
        Assert.That(Occupied(14, Ghost), Is.False);
    }

    [Test]
    public void Frame_GhostSensor_RepeatsOnItsPeriodNotEverySeventhTick()
    {
        Assert.That(Occupied(57, Ghost), Is.True);
        Assert.That(Occupied(21, Ghost), Is.False);
    }

    [Test]
    public void Locos_ReportOneSnapshotPerTrain()
    {
        var locos = _script.Locos(0);

        Assert.That(locos.Select(l => l.Address), Is.EquivalentTo(new[] { 3, 7 }));
    }

    [Test]
    public void Locos_LeadTrain_AcceleratesForwardBlockByBlock()
    {
        Assert.That(Loco(0, 3).Speed, Is.EqualTo(30));
        Assert.That(Loco(0, 3).Forward, Is.True);
        Assert.That(Loco(8, 3).Speed, Is.EqualTo(60));
        Assert.That(Loco(24, 3).Speed, Is.EqualTo(120));
    }

    [Test]
    public void Locos_LeadTrain_ReversesWithoutStoppingBetweenForwardAndBackward()
    {
        Assert.That(Loco(31, 3).Speed, Is.GreaterThan(0));
        Assert.That(Loco(31, 3).Forward, Is.True);
        Assert.That(Loco(32, 3).Speed, Is.GreaterThan(0));
        Assert.That(Loco(32, 3).Forward, Is.False);
    }

    [Test]
    public void Locos_LeadTrain_StopsAtTheEndOfItsCycle()
    {
        Assert.That(Loco(56, 3).Speed, Is.EqualTo(0));
    }

    [Test]
    public void Locos_LeadTrain_IsForwardOnTheOutwardLegsAndReverseOnTheReturnLegs()
    {
        foreach (var tick in new[] { 0, 8, 16, 24, 56 }) Assert.That(Loco(tick, 3).Forward, Is.True, $"tick {tick}");
        foreach (var tick in new[] { 32, 40, 48 }) Assert.That(Loco(tick, 3).Forward, Is.False, $"tick {tick}");
    }

    [Test]
    public void Locos_ShuttleTrain_IsForwardWhileOutboundAndReverseOnTheWayBack()
    {
        foreach (var tick in new[] { 0, 10, 20 }) Assert.That(Loco(tick, 7).Forward, Is.True, $"tick {tick}");
        Assert.That(Loco(26, 7).Forward, Is.False);
    }

    [Test]
    public void Locos_ReportEachTrainsOwnSpeedStepRange()
    {
        Assert.That(Loco(0, 3).MaxSpeed, Is.EqualTo(126));
        Assert.That(Loco(0, 7).MaxSpeed, Is.EqualTo(28));
    }

    [Test]
    public void Locos_ShuttleTrain_DwellsAtTheFarEnd()
    {
        Assert.That(Loco(20, 7).Speed, Is.EqualTo(0));
        Assert.That(Loco(26, 7).Forward, Is.False);
    }

    [Test]
    public void Locos_LeadTrain_OverOneRun_BuildsASingleIntervalSpanningTheReversal()
    {
        var registry = new IntervalSourceRegistry();
        var ingest = new LocoIngest(registry);
        for (var tick = 0; tick <= 52; tick += 4)
        {
            ingest.Apply(Loco(tick, 3), DateTimeOffset.UnixEpoch.AddSeconds(tick));
        }

        var loco = (LocoIntervalSource)registry.Find("loco:3")!;
        var samples = loco.Intervals.Single().Samples;
        Assert.That(samples.Any(s => s.Forward), Is.True);
        Assert.That(samples.Any(s => !s.Forward), Is.True);
    }

    [Test]
    public void System_CurrentAndTemperatureRampWithTick()
    {
        Assert.That(_script.System(10).MainCurrentMilliamps, Is.EqualTo(310));
        Assert.That(_script.System(12).TemperatureCelsius, Is.EqualTo(32));
        Assert.That(_script.System(10).SupplyVoltageMillivolts, Is.EqualTo(15000));
    }

    [Test]
    public void System_ShortCircuit_FiresEverySixtiethTickButNotAtZero()
    {
        Assert.That(_script.System(0).ShortCircuit, Is.False);
        Assert.That(_script.System(60).ShortCircuit, Is.True);
    }

    [Test]
    public void System_ReportsNoOtherFaults()
    {
        var system = _script.System(60);

        Assert.That(system.EmergencyStop, Is.False);
        Assert.That(system.PowerLost, Is.False);
        Assert.That(system.HighTemperature, Is.False);
    }

    [Test]
    public void System_ProgrammingMode_ActiveInItsWindow()
    {
        Assert.That(_script.System(0).ProgrammingMode, Is.False);
        Assert.That(_script.System(20).ProgrammingMode, Is.True);
    }

    [Test]
    public void System_TrackVoltageOff_ActiveInItsWindow()
    {
        Assert.That(_script.System(0).TrackVoltageOff, Is.False);
        Assert.That(_script.System(40).TrackVoltageOff, Is.True);
        Assert.That(_script.System(55).TrackVoltageOff, Is.False);
    }

    [Test]
    public void System_ProgrammingMode_ClearsAtTheEndOfItsWindow()
    {
        Assert.That(_script.System(30).ProgrammingMode, Is.False);
    }

    [Test]
    public void Turnout_FollowsItsSwitchingCadence()
    {
        Assert.That(_script.Turnout(0).Position, Is.EqualTo(TurnoutPosition.Output1));
        Assert.That(_script.Turnout(1).Position, Is.EqualTo(TurnoutPosition.Output1));
        Assert.That(_script.Turnout(18).Position, Is.EqualTo(TurnoutPosition.Output2));
        Assert.That(_script.Turnout(0).Address, Is.EqualTo(5));
    }
}
