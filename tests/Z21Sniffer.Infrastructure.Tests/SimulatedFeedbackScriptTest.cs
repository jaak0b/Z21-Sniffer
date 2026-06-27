using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Infrastructure.Simulation;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class SimulatedFeedbackScriptTest
{
    private readonly SimulatedFeedbackScript _script = new();
    private static readonly SensorKey Arrival = new(1, 1);
    private static readonly SensorKey Platform = new(1, 2);

    private bool Occupied(int tick, SensorKey sensor) => _script.Frame(tick).Single(s => s.Sensor == sensor).Occupied;

    [Test]
    public void Frame_ReportsEverySensorEveryTick()
    {
        Assert.That(_script.Frame(0), Has.Count.EqualTo(3));
        Assert.That(_script.Frame(123), Has.Count.EqualTo(3));
    }

    [Test]
    public void Frame_GhostSensor_FlapsOnMultiplesOfSeven()
    {
        var ghost = new SensorKey(2, 3);

        Assert.That(_script.Frame(7).Single(s => s.Sensor == ghost).Occupied, Is.True);
        Assert.That(_script.Frame(8).Single(s => s.Sensor == ghost).Occupied, Is.False);
    }

    [Test]
    public void Frame_ArrivalSensor_OccupiedEarlyThenClears()
    {
        var arrival = new SensorKey(1, 1);

        Assert.That(_script.Frame(0).Single(s => s.Sensor == arrival).Occupied, Is.True);
        Assert.That(_script.Frame(25).Single(s => s.Sensor == arrival).Occupied, Is.False);
    }

    [Test]
    public void Loco_RampsFromZeroToPeakAndBackThroughZero()
    {
        Assert.That(_script.Loco(96).Speed, Is.EqualTo(0));
        Assert.That(_script.Loco(12).Speed, Is.EqualTo(30));
        Assert.That(_script.Loco(48).Speed, Is.EqualTo(120));
        Assert.That(_script.Loco(84).Speed, Is.EqualTo(30));
    }

    [Test]
    public void Loco_ReportsFullSpeedStepRange()
    {
        Assert.That(_script.Loco(12).MaxSpeed, Is.EqualTo(126));
    }

    [Test]
    public void Loco_KeepsDirectionWithinACycleThenFlips()
    {
        Assert.That(_script.Loco(12).Forward, Is.True);
        Assert.That(_script.Loco(12).Forward, Is.EqualTo(_script.Loco(84).Forward));
        Assert.That(_script.Loco(12).Forward, Is.Not.EqualTo(_script.Loco(108).Forward));
        Assert.That(_script.Loco(192).Forward, Is.True);
    }

    [Test]
    public void Frame_ArrivalSensor_OccupiedAcrossTheFirstTwentyTicks()
    {
        Assert.That(Occupied(0, Arrival), Is.True);
        Assert.That(Occupied(19, Arrival), Is.True);
        Assert.That(Occupied(20, Arrival), Is.False);
        Assert.That(Occupied(39, Arrival), Is.False);
    }

    [Test]
    public void Frame_PlatformSensor_OccupiedFromEightToThirty()
    {
        Assert.That(Occupied(7, Platform), Is.False);
        Assert.That(Occupied(8, Platform), Is.True);
        Assert.That(Occupied(29, Platform), Is.True);
        Assert.That(Occupied(30, Platform), Is.False);
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
        Assert.That(system.TrackVoltageOff, Is.False);
        Assert.That(system.PowerLost, Is.False);
        Assert.That(system.HighTemperature, Is.False);
    }

    [Test]
    public void Loco_OverACycle_BuildsAMultiSampleInterval()
    {
        var registry = new IntervalSourceRegistry();
        var ingest = new LocoIngest(registry);
        for (var tick = 12; tick <= 84; tick += 12)
        {
            ingest.Apply(_script.Loco(tick), DateTimeOffset.UnixEpoch.AddSeconds(tick));
        }

        var loco = (LocoIntervalSource)registry.Find("loco:3")!;
        Assert.That(loco.Intervals.Single().Samples.Count, Is.GreaterThan(1));
    }
}
