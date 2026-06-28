using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class SystemCurrentSourceTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private SystemCurrentSource _source = null!;

    [SetUp]
    public void SetUp() => _source = new SystemCurrentSource { Id = "systemcurrent" };

    [Test]
    public void Apply_FirstSample_OpensOneIntervalWithTheSampleAndMax()
    {
        _source.Apply(milliamps: 800, maxCurrentMilliamps: 3200, T0);

        var interval = _source.Intervals.Single();
        Assert.That(interval.IsOpen, Is.True);
        Assert.That(interval.MaxCurrentMilliamps, Is.EqualTo(3200));
        Assert.That(interval.Samples.Single().Milliamps, Is.EqualTo(800));
        Assert.That(interval.Samples.Single().At, Is.EqualTo(T0));
    }

    [Test]
    public void Apply_FurtherSamples_AppendToTheSameOpenInterval()
    {
        _source.Apply(800, 3200, T0);
        _source.Apply(950, 3200, T0.AddSeconds(1));
        _source.Apply(0, 3200, T0.AddSeconds(2));

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        Assert.That(_source.Intervals.Single().IsOpen, Is.True);
        Assert.That(_source.Intervals.Single().Samples.Select(s => s.Milliamps), Is.EqualTo(new[] { 800, 950, 0 }));
    }

    [Test]
    public void Apply_LaterMax_RefreshesTheIntervalMaxToTheLatestKnownLimit()
    {
        _source.Apply(800, maxCurrentMilliamps: 3200, T0);
        _source.Apply(900, maxCurrentMilliamps: 7000, T0.AddSeconds(1));

        Assert.That(_source.Intervals.Single().MaxCurrentMilliamps, Is.EqualTo(7000));
    }

    [Test]
    public void CloseOpenIntervals_MarksTheIntervalStoppedAndAFurtherSampleOpensANewOne()
    {
        _source.Apply(800, 3200, T0);
        _source.CloseOpenIntervals(T0.AddSeconds(5), IntervalEndReason.Stopped);
        _source.Apply(600, 3200, T0.AddSeconds(6));

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].IsOpen, Is.False);
        Assert.That(_source.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.Stopped));
        Assert.That(_source.Intervals[0].End, Is.EqualTo(T0.AddSeconds(5)));
        Assert.That(_source.Intervals[1].IsOpen, Is.True);
    }

    [Test]
    public void IntervalType_IsSystemCurrentInterval()
    {
        Assert.That(_source.IntervalType, Is.EqualTo(typeof(SystemCurrentInterval)));
    }
}
