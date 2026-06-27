using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class LocoIngestTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private IntervalSourceRegistry _registry = null!;
    private LocoIngest _ingest = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new IntervalSourceRegistry();
        _ingest = new LocoIngest(_registry);
    }

    [Test]
    public void Apply_CreatesLocoSourceKeyedByAddress()
    {
        _ingest.Apply(new LocoSnapshot(482, 40, Forward: true, MaxSpeed: 126), T0);

        var source = (LocoIntervalSource)_registry.Find("loco:482")!;
        Assert.That(source.Address, Is.EqualTo(482));
        Assert.That(source.Intervals.Single().Samples.Single().Speed, Is.EqualTo(40));
    }

    [Test]
    public void Apply_SameAddressTwice_ReusesSource()
    {
        _ingest.Apply(new LocoSnapshot(482, 40, Forward: true, MaxSpeed: 126), T0);
        _ingest.Apply(new LocoSnapshot(482, 60, Forward: true, MaxSpeed: 126), T0.AddSeconds(1));

        Assert.That(_registry.Sources, Has.Count.EqualTo(1));
    }

    [Test]
    public void Apply_DifferentAddresses_CreateSeparateSources()
    {
        _ingest.Apply(new LocoSnapshot(3, 10, Forward: true, MaxSpeed: 126), T0);
        _ingest.Apply(new LocoSnapshot(7, 20, Forward: true, MaxSpeed: 126), T0);

        Assert.That(_registry.Sources, Has.Count.EqualTo(2));
    }
}
