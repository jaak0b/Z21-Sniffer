using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class ConnectionSourceTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private ConnectionSource _source = null!;

    [SetUp]
    public void SetUp() => _source = new ConnectionSource();

    [Test]
    public void Set_FirstState_OpensIntervalCarryingConnected()
    {
        _source.Set(connected: true, T0);

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        var interval = _source.Intervals[0];
        Assert.That(interval.Connected, Is.True);
        Assert.That(interval.Start, Is.EqualTo(T0));
        Assert.That(interval.IsOpen, Is.True);
    }

    [Test]
    public void Set_Transition_ClosesPreviousAndOpensNew()
    {
        _source.Set(connected: true, T0);
        var at = T0 + TimeSpan.FromSeconds(5);

        _source.Set(connected: false, at);

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].End, Is.EqualTo(at));
        Assert.That(_source.Intervals[0].Connected, Is.True);
        Assert.That(_source.Intervals[1].Connected, Is.False);
        Assert.That(_source.Intervals[1].Start, Is.EqualTo(at));
        Assert.That(_source.Intervals[1].IsOpen, Is.True);
    }

    [Test]
    public void Set_UnchangedState_IsNoOp()
    {
        _source.Set(connected: true, T0);

        _source.Set(connected: true, T0 + TimeSpan.FromSeconds(2));

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        Assert.That(_source.Intervals[0].IsOpen, Is.True);
    }
}
