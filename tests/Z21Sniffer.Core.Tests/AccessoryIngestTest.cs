using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class AccessoryIngestTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private IntervalSourceRegistry _registry = null!;
    private AccessoryIngest _ingest = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new IntervalSourceRegistry();
        _ingest = new AccessoryIngest(_registry);
    }

    [Test]
    public void Apply_FirstPosition_CreatesSourceKeyedByAddressAndRecords()
    {
        _ingest.Apply(new TurnoutSnapshot(Address: 12, TurnoutPosition.Output1), T0);

        var source = _registry.Find("accessory:12") as AccessorySource;
        Assert.That(source, Is.Not.Null);
        Assert.That(source!.Address, Is.EqualTo(12));
        Assert.That(source.Intervals, Has.Count.EqualTo(1));
        Assert.That(source.Intervals[0].Position, Is.EqualTo(TurnoutPosition.Output1));
    }

    [Test]
    public void Apply_SameAddressTwice_ReusesTheSameSource()
    {
        _ingest.Apply(new TurnoutSnapshot(12, TurnoutPosition.Output1), T0);
        _ingest.Apply(new TurnoutSnapshot(12, TurnoutPosition.Output2), T0 + TimeSpan.FromSeconds(1));

        Assert.That(_registry.Sources, Has.Count.EqualTo(1));
        Assert.That(_registry.Find("accessory:12")!.Intervals, Has.Count.EqualTo(2));
    }

    [Test]
    public void Apply_UnknownForUnseenAddress_CreatesNoSource()
    {
        _ingest.Apply(new TurnoutSnapshot(12, TurnoutPosition.Unknown), T0);

        Assert.That(_registry.Find("accessory:12"), Is.Null);
    }

    [Test]
    public void Apply_UnknownAfterAKnownPosition_ClosesTheOpenInterval()
    {
        _ingest.Apply(new TurnoutSnapshot(12, TurnoutPosition.Output1), T0);
        var at = T0 + TimeSpan.FromSeconds(2);

        _ingest.Apply(new TurnoutSnapshot(12, TurnoutPosition.Unknown), at);

        var source = _registry.Find("accessory:12")!;
        Assert.That(source.Intervals, Has.Count.EqualTo(1));
        Assert.That(source.Intervals[0].IsOpen, Is.False);
    }
}
