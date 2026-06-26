using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class FeedbackSensorIngestTest
{
    private static readonly SensorKey SensorA = new(Module: 1, Contact: 1);
    private static readonly SensorKey SensorB = new(Module: 1, Contact: 2);
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private IntervalSourceRegistry _registry = null!;
    private FeedbackSensorIngest _ingest = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new IntervalSourceRegistry();
        _ingest = new FeedbackSensorIngest(_registry);
    }

    private static IReadOnlyList<SensorState> Frame(params SensorState[] states) => states;

    private FeedbackSensorSource? SourceFor(SensorKey sensor) =>
        _registry.Sources.OfType<FeedbackSensorSource>().FirstOrDefault(source => source.Sensor == sensor);

    [Test]
    public void Apply_RisingEdge_CreatesSensorSourceWithOpenInterval()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);

        var source = SourceFor(SensorA);
        Assert.That(source, Is.Not.Null);
        Assert.That(source!.Intervals, Has.Count.EqualTo(1));
        Assert.That(source.Intervals[0].IsOpen, Is.True);
        Assert.That(source.Intervals[0].Sensor, Is.EqualTo(SensorA));
    }

    [Test]
    public void Apply_FallingEdge_ClosesInterval()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);
        var at = T0 + TimeSpan.FromSeconds(3);

        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: false)), at);

        var interval = SourceFor(SensorA)!.Intervals[0];
        Assert.That(interval.End, Is.EqualTo(at));
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
    }

    [Test]
    public void Apply_InitialClear_CreatesNoSource()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: false)), T0);

        Assert.That(_registry.Sources, Is.Empty);
    }

    [Test]
    public void Apply_RepeatedOccupied_DoesNotOpenSecondInterval()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0 + TimeSpan.FromSeconds(1));

        Assert.That(SourceFor(SensorA)!.Intervals, Has.Count.EqualTo(1));
    }

    [Test]
    public void Apply_DifferentSensors_ProduceSeparateSources()
    {
        _ingest.Apply(Frame(
            new SensorState(SensorA, Occupied: true),
            new SensorState(SensorB, Occupied: true)), T0);

        Assert.That(_registry.Sources, Has.Count.EqualTo(2));
        Assert.That(SourceFor(SensorA), Is.Not.Null);
        Assert.That(SourceFor(SensorB), Is.Not.Null);
    }

    [Test]
    public void Apply_RisingEdge_RaisesEdgeDetectedOccupied()
    {
        SensorEdge? edge = null;
        _ingest.EdgeDetected += (_, e) => edge = e;

        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);

        Assert.That(edge, Is.Not.Null);
        Assert.That(edge!.Sensor, Is.EqualTo(SensorA));
        Assert.That(edge.Occupied, Is.True);
        Assert.That(edge.At, Is.EqualTo(T0));
        Assert.That(edge.Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public void Apply_NoChange_DoesNotRaiseEdgeDetected()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);
        var raised = 0;
        _ingest.EdgeDetected += (_, _) => raised++;

        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0 + TimeSpan.FromSeconds(1));

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Apply_ClearForUnknownSensor_DoesNotCreateSource()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: false)), T0 + TimeSpan.FromSeconds(1));

        _ingest.Apply(Frame(new SensorState(SensorB, Occupied: false)), T0 + TimeSpan.FromSeconds(2));

        Assert.That(SourceFor(SensorB), Is.Null);
    }

    [Test]
    public void Apply_AfterRegistryCleared_OccupiedAgain_OpensFreshInterval()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);
        _registry.Clear();

        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0 + TimeSpan.FromSeconds(1));

        Assert.That(SourceFor(SensorA), Is.Not.Null);
        Assert.That(SourceFor(SensorA)!.Intervals, Has.Count.EqualTo(1));
        Assert.That(SourceFor(SensorA)!.Intervals[0].IsOpen, Is.True);
    }

    [Test]
    public void Apply_AfterSourceRemoved_OccupiedAgain_ReappearsForSensor()
    {
        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0);
        _registry.Remove(SourceFor(SensorA)!);

        _ingest.Apply(Frame(new SensorState(SensorA, Occupied: true)), T0 + TimeSpan.FromSeconds(1));

        Assert.That(SourceFor(SensorA), Is.Not.Null);
    }
}
