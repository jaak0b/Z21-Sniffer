using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class LocoIntervalSourceTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private LocoIntervalSource _source = null!;

    [SetUp]
    public void SetUp() => _source = new LocoIntervalSource { Id = "loco:482", Address = 482 };

    [Test]
    public void Apply_PositiveSpeed_OpensIntervalWithDirectionAndMax()
    {
        _source.Apply(speed: 40, forward: true, maxSpeed: 126, T0);

        var interval = _source.Intervals.Single();
        Assert.That(interval.IsOpen, Is.True);
        Assert.That(interval.Forward, Is.True);
        Assert.That(interval.MaxSpeed, Is.EqualTo(126));
        Assert.That(interval.Samples.Single().Speed, Is.EqualTo(40));
        Assert.That(interval.Samples.Single().At, Is.EqualTo(T0));
    }

    [Test]
    public void Apply_SameDirectionMultipleSpeeds_AppendsSamplesToOneInterval()
    {
        _source.Apply(40, forward: true, maxSpeed: 126, T0);
        _source.Apply(60, forward: true, maxSpeed: 126, T0.AddSeconds(1));

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        Assert.That(_source.Intervals.Single().Samples.Select(s => s.Speed), Is.EqualTo(new[] { 40, 60 }));
    }

    [Test]
    public void Apply_SpeedZero_ClosesIntervalAsFallingEdge()
    {
        _source.Apply(40, forward: true, maxSpeed: 126, T0);
        _source.Apply(0, forward: true, maxSpeed: 126, T0.AddSeconds(2));

        var interval = _source.Intervals.Single();
        Assert.That(interval.IsOpen, Is.False);
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(interval.End, Is.EqualTo(T0.AddSeconds(2)));
    }

    [Test]
    public void Apply_SpeedZeroWithNoOpenInterval_DoesNothing()
    {
        _source.Apply(0, forward: true, maxSpeed: 126, T0);

        Assert.That(_source.Intervals, Is.Empty);
    }

    [Test]
    public void Apply_DirectionFlipWithoutZero_ClosesAndReopensFlipped()
    {
        _source.Apply(40, forward: true, maxSpeed: 126, T0);
        _source.Apply(40, forward: false, maxSpeed: 126, T0.AddSeconds(1));

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        var first = _source.Intervals[0];
        var second = _source.Intervals[1];
        Assert.That(first.IsOpen, Is.False);
        Assert.That(first.EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(first.End, Is.EqualTo(T0.AddSeconds(1)));
        Assert.That(second.Forward, Is.False);
        Assert.That(second.IsOpen, Is.True);
        Assert.That(second.Samples.Single().Speed, Is.EqualTo(40));
    }

    [Test]
    public void Label_DefaultsToAddress()
    {
        Assert.That(_source.Label, Is.EqualTo("482"));
    }

    [Test]
    public void Label_WhenSet_PersistsViaKeyValueStore()
    {
        _source.Label = "Express";

        Assert.That(_source.Label, Is.EqualTo("Express"));
    }

    [Test]
    public void IntervalType_IsLocoInterval()
    {
        Assert.That(_source.IntervalType, Is.EqualTo(typeof(LocoInterval)));
    }
}
