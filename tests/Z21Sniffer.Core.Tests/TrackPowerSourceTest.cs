using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class TrackPowerSourceTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private TrackPowerSource _source = null!;

    [SetUp]
    public void SetUp() => _source = new TrackPowerSource { Id = "trackpower" };

    private static SystemSnapshot Snap(bool shortCircuit = false, bool programming = false, bool trackVoltageOff = false) =>
        new(0, 0, 0, ShortCircuit: shortCircuit, EmergencyStop: false, TrackVoltageOff: trackVoltageOff,
            ProgrammingMode: programming, PowerLost: false, HighTemperature: false);

    [Test]
    public void Set_FirstStatus_OpensInterval()
    {
        _source.Set(TrackPowerStatus.On, T0);

        var interval = _source.Intervals.Single();
        Assert.That(interval.IsOpen, Is.True);
        Assert.That(interval.Status, Is.EqualTo(TrackPowerStatus.On));
        Assert.That(interval.Start, Is.EqualTo(T0));
    }

    [Test]
    public void Set_SameStatus_DoesNotOpenAnother()
    {
        _source.Set(TrackPowerStatus.On, T0);
        _source.Set(TrackPowerStatus.On, T0.AddSeconds(1));

        Assert.That(_source.Intervals, Has.Count.EqualTo(1));
    }

    [Test]
    public void Set_ChangedStatus_ClosesAsFallingEdgeAndOpensNew()
    {
        _source.Set(TrackPowerStatus.On, T0);
        _source.Set(TrackPowerStatus.Off, T0.AddSeconds(2));

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        var first = _source.Intervals[0];
        var second = _source.Intervals[1];
        Assert.That(first.IsOpen, Is.False);
        Assert.That(first.EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(first.End, Is.EqualTo(T0.AddSeconds(2)));
        Assert.That(second.Status, Is.EqualTo(TrackPowerStatus.Off));
        Assert.That(second.IsOpen, Is.True);
    }

    [Test]
    public void Apply_NormalSnapshot_DerivesOn()
    {
        _source.Apply(Snap(), T0);

        Assert.That(_source.Intervals.Single().Status, Is.EqualTo(TrackPowerStatus.On));
    }

    [Test]
    public void Apply_TrackVoltageOff_DerivesOff()
    {
        _source.Apply(Snap(trackVoltageOff: true), T0);

        Assert.That(_source.Intervals.Single().Status, Is.EqualTo(TrackPowerStatus.Off));
    }

    [Test]
    public void Apply_ProgrammingMode_DerivesProgramming()
    {
        _source.Apply(Snap(programming: true), T0);

        Assert.That(_source.Intervals.Single().Status, Is.EqualTo(TrackPowerStatus.Programming));
    }

    [Test]
    public void Apply_ShortCircuit_DerivesShort()
    {
        _source.Apply(Snap(shortCircuit: true), T0);

        Assert.That(_source.Intervals.Single().Status, Is.EqualTo(TrackPowerStatus.Short));
    }

    [Test]
    public void Apply_ShortCircuit_TakesPrecedenceOverProgrammingAndOff()
    {
        _source.Apply(Snap(shortCircuit: true, programming: true, trackVoltageOff: true), T0);

        Assert.That(_source.Intervals.Single().Status, Is.EqualTo(TrackPowerStatus.Short));
    }

    [Test]
    public void Apply_ProgrammingMode_TakesPrecedenceOverOff()
    {
        _source.Apply(Snap(programming: true, trackVoltageOff: true), T0);

        Assert.That(_source.Intervals.Single().Status, Is.EqualTo(TrackPowerStatus.Programming));
    }

    [Test]
    public void Apply_StatusChange_ClosesPreviousAndOpensNew()
    {
        _source.Apply(Snap(), T0);
        _source.Apply(Snap(shortCircuit: true), T0.AddSeconds(3));

        Assert.That(_source.Intervals, Has.Count.EqualTo(2));
        Assert.That(_source.Intervals[0].Status, Is.EqualTo(TrackPowerStatus.On));
        Assert.That(_source.Intervals[0].EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
        Assert.That(_source.Intervals[1].Status, Is.EqualTo(TrackPowerStatus.Short));
    }

    [Test]
    public void IntervalType_IsTrackPowerInterval()
    {
        Assert.That(_source.IntervalType, Is.EqualTo(typeof(TrackPowerInterval)));
    }
}
