using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class FeedbackSensorSourceTest
{
    private static readonly SensorKey Sensor = new(Module: 1, Contact: 1);
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private FeedbackSensorSource _source = null!;

    [SetUp]
    public void SetUp() => _source = new FeedbackSensorSource { Sensor = Sensor };

    [Test]
    public void Apply_RisingEdge_OpensIntervalCarryingTheSensor()
    {
        _source.Apply(occupied: true, T0);

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        var interval = _source.Intervals[0];
        Assert.That(interval.Sensor, Is.EqualTo(Sensor));
        Assert.That(interval.Start, Is.EqualTo(T0));
        Assert.That(interval.IsOpen, Is.True);
    }

    [Test]
    public void Apply_FallingEdge_ClosesIntervalAsFallingEdge()
    {
        _source.Apply(occupied: true, T0);
        var at = T0 + TimeSpan.FromSeconds(3);

        _source.Apply(occupied: false, at);

        var interval = _source.Intervals[0];
        Assert.That(interval.End, Is.EqualTo(at));
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
    }

    [Test]
    public void Apply_RepeatedOccupied_DoesNotOpenSecondInterval()
    {
        _source.Apply(occupied: true, T0);
        _source.Apply(occupied: true, T0 + TimeSpan.FromSeconds(1));

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
    }

    [Test]
    public void Label_DefaultsToSensorAddress()
    {
        Assert.That(_source.Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public void Label_Set_PersistsToBoundStoreKeyedById()
    {
        var store = new InMemoryKeyValueStore();
        _source.Id = "sensor:1.1";
        _source.UsePersistence(store);

        _source.Label = "Yard 3";

        Assert.That(store.GetValue<string>("sensor:1.1/label"), Is.EqualTo("Yard 3"));
        Assert.That(_source.Label, Is.EqualTo("Yard 3"));
    }

    [Test]
    public void Label_PersistedValueOverridesDefault()
    {
        var store = new InMemoryKeyValueStore();
        store.SetValue("sensor:1.1/label", "Station 2");
        _source.Id = "sensor:1.1";
        _source.UsePersistence(store);

        Assert.That(_source.Label, Is.EqualTo("Station 2"));
    }

    [Test]
    public void Apply_InitialClear_CreatesNothing()
    {
        _source.Apply(occupied: false, T0);

        Assert.That(_source.Intervals, Is.Empty);
    }

    [Test]
    public void Apply_RapidFlap_ProducesMultipleClosedIntervals()
    {
        var t = T0;
        for (var i = 0; i < 3; i++)
        {
            _source.Apply(occupied: true, t);
            t += TimeSpan.FromMilliseconds(50);
            _source.Apply(occupied: false, t);
            t += TimeSpan.FromMilliseconds(50);
        }

        Assert.That(_source.Intervals, Has.Count.EqualTo(3));
        Assert.That(_source.Intervals, Has.All.Matches<FeedbackSensorInterval>(i => !i.IsOpen));
    }
}
