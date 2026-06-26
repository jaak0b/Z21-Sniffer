using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Infrastructure.Simulation;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class SimulatedFeedbackScriptTest
{
    private readonly SimulatedFeedbackScript _script = new();

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
}
