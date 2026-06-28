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
    public void Apply_FirstSample_OpensOneIntervalCarryingTheDeviceIdentityAndSample()
    {
        _source.Apply(milliamps: 800, typeCode: 516, deviceName: "z21 start", maxCurrentMilliamps: 2000, T0);

        var interval = _source.Intervals.Single();
        Assert.That(interval.IsOpen, Is.True);
        Assert.That(interval.TypeCode, Is.EqualTo(516));
        Assert.That(interval.DeviceName, Is.EqualTo("z21 start"));
        Assert.That(interval.MaxCurrentMilliamps, Is.EqualTo(2000));
        Assert.That(interval.Samples.Single().Milliamps, Is.EqualTo(800));
        Assert.That(interval.Samples.Single().At, Is.EqualTo(T0));
    }

    [Test]
    public void Apply_SameDevice_AppendsToTheSameOpenInterval()
    {
        _source.Apply(800, 516, "z21 start", 2000, T0);
        _source.Apply(950, 516, "z21 start", 2000, T0.AddSeconds(1));

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        Assert.That(_source.Intervals.Single().Samples.Select(s => s.Milliamps), Is.EqualTo(new[] { 800, 950 }));
    }

    [Test]
    public void Apply_DifferentHardwareId_ClosesTheCurrentIntervalAndOpensANewOne()
    {
        _source.Apply(800, typeCode: 516, deviceName: "z21 start", maxCurrentMilliamps: 2000, T0);
        _source.Apply(4200, typeCode: 529, deviceName: "Z21 XL", maxCurrentMilliamps: 6000, T0.AddSeconds(1));

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].IsOpen, Is.False);
        Assert.That(_source.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(_source.Intervals[0].End, Is.EqualTo(T0.AddSeconds(1)));
        Assert.That(_source.Intervals[1].TypeCode, Is.EqualTo(529));
        Assert.That(_source.Intervals[1].MaxCurrentMilliamps, Is.EqualTo(6000));
        Assert.That(_source.Intervals[1].Samples.Single().Milliamps, Is.EqualTo(4200));
    }

    [Test]
    public void Apply_SameUnknownDevice_StaysOneIntervalWithNullNameAndMax()
    {
        _source.Apply(800, typeCode: 0, deviceName: null, maxCurrentMilliamps: null, T0);
        _source.Apply(820, typeCode: 0, deviceName: null, maxCurrentMilliamps: null, T0.AddSeconds(1));

        var interval = _source.Intervals.Single();
        Assert.That(interval.TypeCode, Is.EqualTo(0));
        Assert.That(interval.DeviceName, Is.Null);
        Assert.That(interval.MaxCurrentMilliamps, Is.Null);
        Assert.That(interval.Samples, Has.Count.EqualTo(2));
    }

    [Test]
    public void CloseOpenIntervals_MarksTheIntervalStoppedAndAFurtherSampleOpensANewOne()
    {
        _source.Apply(800, 516, "z21 start", 2000, T0);
        _source.CloseOpenIntervals(T0.AddSeconds(5), IntervalEndReason.Stopped);
        _source.Apply(600, 516, "z21 start", 2000, T0.AddSeconds(6));

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].IsOpen, Is.False);
        Assert.That(_source.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.Stopped));
        Assert.That(_source.Intervals[1].IsOpen, Is.True);
    }

    [Test]
    public void IntervalType_IsSystemCurrentInterval()
    {
        Assert.That(_source.IntervalType, Is.EqualTo(typeof(SystemCurrentInterval)));
    }

    [Test]
    public void HighlightsShortIntervals_IsFalse() =>
        Assert.That(_source.HighlightsShortIntervals, Is.False);
}
