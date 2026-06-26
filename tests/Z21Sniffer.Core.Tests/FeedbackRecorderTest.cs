using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class FeedbackRecorderTest
{
    private static readonly SensorKey SensorA = new(Module: 1, Contact: 1);
    private static readonly SensorKey SensorB = new(Module: 1, Contact: 2);

    private FakeClock _clock = null!;
    private FeedbackRecorder _recorder = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new FakeClock();
        _recorder = new FeedbackRecorder(_clock);
    }

    private static IReadOnlyList<SensorState> Frame(params SensorState[] states) => states;

    [Test]
    public void Apply_RisingEdge_OpensInterval()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        Assert.That(_recorder.Intervals, Has.Count.EqualTo(1));
        var interval = _recorder.Intervals[0];
        Assert.That(interval.Sensor, Is.EqualTo(SensorA));
        Assert.That(interval.Start, Is.EqualTo(_clock.Now));
        Assert.That(interval.IsOpen, Is.True);
    }

    [Test]
    public void Apply_FallingEdge_ClosesInterval()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        var start = _clock.Now;
        _clock.Advance(TimeSpan.FromSeconds(3));
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: false)));

        Assert.That(_recorder.Intervals, Has.Count.EqualTo(1));
        var interval = _recorder.Intervals[0];
        Assert.That(interval.Start, Is.EqualTo(start));
        Assert.That(interval.End, Is.EqualTo(_clock.Now));
        Assert.That(interval.Duration, Is.EqualTo(TimeSpan.FromSeconds(3)));
    }

    [Test]
    public void Apply_RapidFlap_ProducesMultipleIntervals()
    {
        for (var i = 0; i < 3; i++)
        {
            _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
            _clock.Advance(TimeSpan.FromMilliseconds(50));
            _recorder.Apply(Frame(new SensorState(SensorA, Occupied: false)));
            _clock.Advance(TimeSpan.FromMilliseconds(50));
        }

        Assert.That(_recorder.Intervals, Has.Count.EqualTo(3));
        Assert.That(_recorder.Intervals, Has.All.Matches<SensorInterval>(i => !i.IsOpen));
    }

    [Test]
    public void Apply_RepeatedOccupiedState_DoesNotOpenSecondInterval()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        _clock.Advance(TimeSpan.FromSeconds(1));
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        Assert.That(_recorder.Intervals, Has.Count.EqualTo(1));
        Assert.That(_recorder.Intervals[0].IsOpen, Is.True);
    }

    [Test]
    public void Apply_InitialClearState_CreatesNothing()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: false)));

        Assert.That(_recorder.Intervals, Is.Empty);
    }

    [Test]
    public void Apply_DifferentSensors_TrackedIndependently()
    {
        _recorder.Apply(Frame(
            new SensorState(SensorA, Occupied: true),
            new SensorState(SensorB, Occupied: false)));
        _clock.Advance(TimeSpan.FromSeconds(1));
        _recorder.Apply(Frame(
            new SensorState(SensorA, Occupied: true),
            new SensorState(SensorB, Occupied: true)));

        Assert.That(_recorder.Intervals.Count, Is.EqualTo(2));
        Assert.That(_recorder.Intervals.Count(i => i.Sensor == SensorA), Is.EqualTo(1));
        Assert.That(_recorder.Intervals.Count(i => i.Sensor == SensorB), Is.EqualTo(1));
    }

    [Test]
    public void Apply_RisingEdge_RaisesChanged()
    {
        var raised = 0;
        _recorder.Changed += (_, _) => raised++;

        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Apply_NoEdge_DoesNotRaiseChanged()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: false)));
        var raised = 0;
        _recorder.Changed += (_, _) => raised++;

        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: false)));

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Apply_RisingEdge_RaisesEdgeDetectedOccupied()
    {
        SensorEdge? edge = null;
        _recorder.EdgeDetected += (_, e) => edge = e;

        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        Assert.That(edge, Is.Not.Null);
        Assert.That(edge!.Sensor, Is.EqualTo(SensorA));
        Assert.That(edge.Occupied, Is.True);
        Assert.That(edge.At, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void Apply_FallingEdge_RaisesEdgeDetectedClear()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        _clock.Advance(TimeSpan.FromSeconds(2));
        SensorEdge? edge = null;
        _recorder.EdgeDetected += (_, e) => edge = e;

        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: false)));

        Assert.That(edge, Is.Not.Null);
        Assert.That(edge!.Sensor, Is.EqualTo(SensorA));
        Assert.That(edge.Occupied, Is.False);
        Assert.That(edge.At, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void Apply_NoEdge_DoesNotRaiseEdgeDetected()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        var raised = 0;
        _recorder.EdgeDetected += (_, _) => raised++;

        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Clear_RemovesIntervalsAndResetsState()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        _recorder.Clear();
        Assert.That(_recorder.Intervals, Is.Empty);

        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        Assert.That(_recorder.Intervals, Has.Count.EqualTo(1));
    }

    [Test]
    public void Clear_RaisesChanged()
    {
        var raised = 0;
        _recorder.Changed += (_, _) => raised++;

        _recorder.Clear();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void StartedAt_IsConstructionClockTime()
    {
        Assert.That(_recorder.StartedAt, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void Remove_DropsThatSensorsIntervals()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        _recorder.Apply(Frame(new SensorState(SensorB, Occupied: true)));

        _recorder.Remove(SensorA);

        Assert.That(_recorder.Intervals.Select(i => i.Sensor), Is.EqualTo(new[] { SensorB }));
    }

    [Test]
    public void Remove_KeepsAnotherSensorsOpenIntervalCloseable()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true), new SensorState(SensorB, Occupied: true)));

        _recorder.Remove(SensorA);
        _clock.Advance(TimeSpan.FromSeconds(2));
        _recorder.Apply(Frame(new SensorState(SensorB, Occupied: false)));

        var interval = _recorder.Intervals.Single();
        Assert.That(interval.Sensor, Is.EqualTo(SensorB));
        Assert.That(interval.End, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void Remove_RaisesChanged()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        var raised = 0;
        _recorder.Changed += (_, _) => raised++;

        _recorder.Remove(SensorA);

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Remove_AllowsSensorToReappear()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        _recorder.Remove(SensorA);
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        Assert.That(_recorder.Intervals.Count(i => i.Sensor == SensorA), Is.EqualTo(1));
    }

    [Test]
    public void ToSession_CapturesStartTimeAndIntervals()
    {
        var startedAt = _clock.Now;
        _clock.Advance(TimeSpan.FromSeconds(2));
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));

        var session = _recorder.ToSession();

        Assert.That(session.StartedAt, Is.EqualTo(startedAt));
        Assert.That(session.Intervals, Has.Count.EqualTo(1));
    }

    [Test]
    public void Restore_SetsStartTimeAndIntervals()
    {
        var startedAt = _clock.Now - TimeSpan.FromMinutes(5);
        var intervals = new List<SensorInterval>
        {
            new(SensorA, startedAt, startedAt + TimeSpan.FromSeconds(1)),
            new(SensorB, startedAt + TimeSpan.FromSeconds(2), End: null),
        };
        var session = new RecordingSession(startedAt, intervals);

        _recorder.Restore(session);

        Assert.That(_recorder.StartedAt, Is.EqualTo(startedAt));
        Assert.That(_recorder.Intervals, Is.EqualTo(intervals));
    }

    [Test]
    public void Restore_RoundTripsThroughToSession()
    {
        var startedAt = _clock.Now - TimeSpan.FromMinutes(1);
        var session = new RecordingSession(
            startedAt,
            new List<SensorInterval> { new(SensorA, startedAt, startedAt + TimeSpan.FromSeconds(3)) });

        _recorder.Restore(session);
        var round = _recorder.ToSession();

        Assert.That(round.StartedAt, Is.EqualTo(startedAt));
        Assert.That(round.Intervals, Is.EqualTo(session.Intervals));
    }

    [Test]
    public void Restore_DiscardsPreviousIntervals()
    {
        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: true)));
        var startedAt = _clock.Now - TimeSpan.FromMinutes(2);
        var session = new RecordingSession(
            startedAt,
            new List<SensorInterval> { new(SensorB, startedAt, startedAt + TimeSpan.FromSeconds(1)) });

        _recorder.Restore(session);

        Assert.That(_recorder.Intervals.Select(i => i.Sensor), Is.EqualTo(new[] { SensorB }));
    }

    [Test]
    public void Restore_ThenFallingEdge_ClosesTheRestoredOpenInterval()
    {
        var startedAt = _clock.Now - TimeSpan.FromMinutes(1);
        _recorder.Restore(new RecordingSession(
            startedAt,
            new List<SensorInterval> { new(SensorA, startedAt, End: null) }));
        _clock.Advance(TimeSpan.FromSeconds(2));

        _recorder.Apply(Frame(new SensorState(SensorA, Occupied: false)));

        Assert.That(_recorder.Intervals, Has.Count.EqualTo(1));
        Assert.That(_recorder.Intervals[0].End, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void Restore_DiscardsPreviousOccupancy()
    {
        _recorder.Apply(Frame(new SensorState(SensorB, Occupied: true)));
        var startedAt = _clock.Now;

        _recorder.Restore(new RecordingSession(startedAt, new List<SensorInterval>()));
        _recorder.Apply(Frame(new SensorState(SensorB, Occupied: true)));

        Assert.That(_recorder.Intervals, Has.Count.EqualTo(1));
        Assert.That(_recorder.Intervals[0].Sensor, Is.EqualTo(SensorB));
        Assert.That(_recorder.Intervals[0].IsOpen, Is.True);
    }
}
