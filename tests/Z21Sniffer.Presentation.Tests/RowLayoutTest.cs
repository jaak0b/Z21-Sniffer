using Autofac.Features.Indexed;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class RowLayoutTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private RowLayout _layout = null!;

    [SetUp]
    public void SetUp() =>
        _layout = new RowLayout(new FakeIndex<Type, IIntervalChartDrawingStrategy>(new Dictionary<Type, IIntervalChartDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalChartDrawingStrategy(),
            [typeof(LocoInterval)] = new LocoIntervalChartDrawingStrategy(),
        }));

    private static FeedbackSensorSource Sensor(int order) =>
        new() { Id = $"sensor:{order}", Sensor = new SensorKey(1, order) };

    private static LocoIntervalSource Loco(int order) =>
        new() { Id = $"loco:{order}", Address = order };

    [Test]
    public void Compute_StacksRowsInTheGivenOrderUsingPerStrategyHeights()
    {
        var sensor = Sensor(0);
        var loco = Loco(1);

        var rows = _layout.Compute(new IIntervalSource[] { sensor, loco }, zoomFraction: 0);

        Assert.That(rows[0].Source, Is.SameAs(sensor));
        Assert.That(rows[0].Top, Is.EqualTo(0));
        Assert.That(rows[0].Height, Is.EqualTo(26));
        Assert.That(rows[1].Source, Is.SameAs(loco));
        Assert.That(rows[1].Top, Is.EqualTo(26));
        Assert.That(rows[1].Height, Is.EqualTo(34));
    }

    [Test]
    public void Compute_SkipsHiddenSources()
    {
        var sensor = Sensor(0);
        var loco = Loco(1);
        sensor.IsVisible = false;

        var rows = _layout.Compute(new IIntervalSource[] { sensor, loco }, zoomFraction: 0);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Source, Is.SameAs(loco));
        Assert.That(rows[0].Top, Is.EqualTo(0));
    }

    [Test]
    public void SourceAt_ReturnsTheRowContainingY()
    {
        var sensor = Sensor(0);
        var loco = Loco(1);
        var sources = new IIntervalSource[] { sensor, loco };

        Assert.That(_layout.SourceAt(sources, 0, 10), Is.SameAs(sensor));
        Assert.That(_layout.SourceAt(sources, 0, 40), Is.SameAs(loco));
    }

    [Test]
    public void SourceAt_BelowAllRows_IsNull()
    {
        var sources = new IIntervalSource[] { Sensor(0) };

        Assert.That(_layout.SourceAt(sources, 0, 999), Is.Null);
    }

    [Test]
    public void SourceAt_AtTheExactTopOfARow_BelongsToThatRow()
    {
        var sensor = Sensor(0);
        var loco = Loco(1);

        Assert.That(_layout.SourceAt(new IIntervalSource[] { sensor, loco }, 0, 0), Is.SameAs(sensor));
    }

    [Test]
    public void SourceAt_AtTheBoundaryBetweenRows_BelongsToTheLowerRow()
    {
        var sensor = Sensor(0);
        var loco = Loco(1);

        Assert.That(_layout.SourceAt(new IIntervalSource[] { sensor, loco }, 0, 26), Is.SameAs(loco));
    }
}
