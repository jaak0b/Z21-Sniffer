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
        Assert.That(interval.MaxSpeed, Is.EqualTo(126));
        Assert.That(interval.Samples.Single().Speed, Is.EqualTo(40));
        Assert.That(interval.Samples.Single().Forward, Is.True);
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
    public void Apply_DirectionFlipWithoutZero_StaysOneIntervalWithPerSampleDirection()
    {
        _source.Apply(40, forward: true, maxSpeed: 126, T0);
        _source.Apply(40, forward: false, maxSpeed: 126, T0.AddSeconds(1));

        var interval = _source.Intervals.Single();
        Assert.That(interval.IsOpen, Is.True);
        Assert.That(interval.Samples.Select(s => s.Forward), Is.EqualTo(new[] { true, false }));
        Assert.That(interval.Samples.Select(s => s.Speed), Is.EqualTo(new[] { 40, 40 }));
    }

    [Test]
    public void Apply_StopThenReverse_ClosesFirstBarAndOpensASecond()
    {
        _source.Apply(40, forward: true, maxSpeed: 126, T0);
        _source.Apply(0, forward: true, maxSpeed: 126, T0.AddSeconds(1));
        _source.Apply(30, forward: false, maxSpeed: 126, T0.AddSeconds(2));

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(_source.Intervals[1].Samples.Single().Forward, Is.False);
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
    public void Label_PersistsUnderTheSourceScopedKey_AcrossReconstructedSources()
    {
        var store = new InMemoryKeyValueStore();
        _source.UsePersistence(store);
        _source.Label = "Express";

        var reloaded = new LocoIntervalSource { Id = "loco:482", Address = 482 };
        reloaded.UsePersistence(store);

        Assert.That(reloaded.Label, Is.EqualTo("Express"));
    }

    [Test]
    public void HasAlias_ByDefault_IsFalse()
    {
        Assert.That(_source.HasAlias, Is.False);
    }

    [Test]
    public void HasAlias_WhenSetToACustomName_IsTrue()
    {
        _source.Label = "Express";

        Assert.That(_source.HasAlias, Is.True);
    }

    [Test]
    public void HasAlias_WhenSetToTheAddressString_IsFalse()
    {
        _source.Label = "482";

        Assert.That(_source.HasAlias, Is.False);
    }

    [Test]
    public void HasAlias_WhenSetToBlank_IsFalse()
    {
        _source.Label = "   ";

        Assert.That(_source.HasAlias, Is.False);
    }

    [Test]
    public void IntervalType_IsLocoInterval()
    {
        Assert.That(_source.IntervalType, Is.EqualTo(typeof(LocoInterval)));
    }

    [Test]
    public void HighlightsShortIntervals_IsFalse() =>
        Assert.That(_source.HighlightsShortIntervals, Is.False);
}
