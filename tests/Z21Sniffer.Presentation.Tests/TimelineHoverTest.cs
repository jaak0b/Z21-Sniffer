using Autofac.Features.Indexed;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TimelineHoverTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private TimelineHover _hover = null!;

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _hover = new TimelineHover(new FakeIndex<Type, IIntervalChartDrawingStrategy>(new Dictionary<Type, IIntervalChartDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalChartDrawingStrategy(),
            [typeof(LocoInterval)] = new LocoIntervalChartDrawingStrategy(),
        }));
    }

    private static ChartViewport Viewport() => new(T0, T0.AddSeconds(10), Width: 100);

    private static LocoIntervalSource LocoWith(int order, params (double At, int Speed)[] samples)
    {
        var source = new LocoIntervalSource { Id = $"loco:{order}", Address = order, Order = order };
        foreach (var sample in samples) source.Apply(sample.Speed, forward: true, maxSpeed: 100, T0.AddSeconds(sample.At));
        return source;
    }

    [Test]
    public void ValueAt_OverLocoRow_ReportsHeldSpeedRegardlessOfVerticalPositionInRow()
    {
        var loco = LocoWith(0, (2, 40), (6, 90));
        var sources = new IIntervalSource[] { loco };

        var near = _hover.ValueAt(sources, Viewport(), 0, x: 40, y: 2);
        var far = _hover.ValueAt(sources, Viewport(), 0, x: 40, y: 30);

        Assert.That(near, Does.Contain("Speed 40"));
        Assert.That(far, Is.EqualTo(near));
    }

    [Test]
    public void ValueAt_OverASensorBar_DescribesTheSensor()
    {
        var sensor = new FeedbackSensorSource { Id = "sensor:1.1", Sensor = new SensorKey(1, 1), Label = "Yard", Order = 0 };
        sensor.Apply(occupied: true, T0.AddSeconds(1));

        var value = _hover.ValueAt(new IIntervalSource[] { sensor }, Viewport(), 0, x: 50, y: 10);

        Assert.That(value, Does.Contain("Yard"));
    }

    [Test]
    public void ValueAt_WhereNoIntervalCoversTheCursorTime_IsNull()
    {
        var loco = LocoWith(0, (5, 40));

        var value = _hover.ValueAt(new IIntervalSource[] { loco }, Viewport(), 0, x: 10, y: 5);

        Assert.That(value, Is.Null);
    }

    [Test]
    public void ValueAt_BelowEveryRow_IsNull()
    {
        var loco = LocoWith(0, (2, 40));

        Assert.That(_hover.ValueAt(new IIntervalSource[] { loco }, Viewport(), 0, x: 40, y: 999), Is.Null);
    }

    [Test]
    public void ValueAt_ExactlyAtAnIntervalsStart_IsCovered()
    {
        var loco = LocoWith(0, (2, 40));

        Assert.That(_hover.ValueAt(new IIntervalSource[] { loco }, Viewport(), 0, x: 20, y: 5), Does.Contain("Speed 40"));
    }

    [Test]
    public void ValueAt_AfterAClosedIntervalHasEnded_IsNull()
    {
        var loco = new LocoIntervalSource { Id = "loco:0", Address = 0, Order = 0 };
        loco.Apply(40, forward: true, maxSpeed: 100, T0);
        loco.Apply(0, forward: true, maxSpeed: 100, T0.AddSeconds(5));

        Assert.That(_hover.ValueAt(new IIntervalSource[] { loco }, Viewport(), 0, x: 70, y: 5), Is.Null);
    }

    [Test]
    public void ValueAt_BeforeABarsStart_IsNull()
    {
        var sensor = new FeedbackSensorSource { Id = "sensor:1.1", Sensor = new SensorKey(1, 1), Label = "Yard", Order = 0 };
        sensor.Apply(occupied: true, T0.AddSeconds(5));

        Assert.That(_hover.ValueAt(new IIntervalSource[] { sensor }, Viewport(), 0, x: 10, y: 5), Is.Null);
    }

    [Test]
    public void ValueAt_AtTheLiveEdgeOfAnOpenInterval_IsCovered()
    {
        var loco = LocoWith(0, (2, 40));

        Assert.That(_hover.ValueAt(new IIntervalSource[] { loco }, Viewport(), 0, x: 100, y: 5), Does.Contain("Speed 40"));
    }
}
