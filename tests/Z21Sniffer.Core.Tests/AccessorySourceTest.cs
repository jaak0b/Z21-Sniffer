using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class AccessorySourceTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private AccessorySource _source = null!;

    [SetUp]
    public void SetUp() => _source = new AccessorySource { Address = 12 };

    [Test]
    public void Apply_FirstPosition_OpensIntervalCarryingAddressAndPosition()
    {
        _source.Apply(TurnoutPosition.Output1, T0);

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        var interval = _source.Intervals[0];
        Assert.That(interval.Address, Is.EqualTo(12));
        Assert.That(interval.Position, Is.EqualTo(TurnoutPosition.Output1));
        Assert.That(interval.Start, Is.EqualTo(T0));
        Assert.That(interval.IsOpen, Is.True);
    }

    [Test]
    public void Apply_DifferentPosition_ClosesPriorAndOpensNew()
    {
        _source.Apply(TurnoutPosition.Output1, T0);
        var at = T0 + TimeSpan.FromSeconds(2);

        _source.Apply(TurnoutPosition.Output2, at);

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].End, Is.EqualTo(at));
        Assert.That(_source.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(_source.Intervals[1].Position, Is.EqualTo(TurnoutPosition.Output2));
        Assert.That(_source.Intervals[1].IsOpen, Is.True);
    }

    [Test]
    public void Apply_SamePositionAgain_StillClosesPriorAndOpensNew()
    {
        _source.Apply(TurnoutPosition.Output1, T0);
        var at = T0 + TimeSpan.FromSeconds(1);

        _source.Apply(TurnoutPosition.Output1, at);

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].IsOpen, Is.False);
        Assert.That(_source.Intervals[0].End, Is.EqualTo(at));
        Assert.That(_source.Intervals[1].IsOpen, Is.True);
    }

    [Test]
    public void Apply_Unknown_ClosesOpenIntervalAndOpensNothing()
    {
        _source.Apply(TurnoutPosition.Output1, T0);
        var at = T0 + TimeSpan.FromSeconds(3);

        _source.Apply(TurnoutPosition.Unknown, at);

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
        Assert.That(_source.Intervals[0].IsOpen, Is.False);
        Assert.That(_source.Intervals[0].End, Is.EqualTo(at));
        Assert.That(_source.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
    }

    [Test]
    public void Apply_UnknownWithNothingOpen_CreatesNothing()
    {
        _source.Apply(TurnoutPosition.Unknown, T0);

        Assert.That(_source.Intervals, Is.Empty);
    }

    [Test]
    public void Label_DefaultsToAccessoryAddress()
    {
        Assert.That(_source.Label, Is.EqualTo("A12"));
    }
}
