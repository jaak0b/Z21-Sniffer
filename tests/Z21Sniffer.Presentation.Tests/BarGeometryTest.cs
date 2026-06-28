using NUnit.Framework;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class BarGeometryTest
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
    private readonly BarGeometry _geometry = new();

    private BarSpan? Compute(DateTimeOffset start, DateTimeOffset? end, DateTimeOffset now) =>
        _geometry.Compute(T0, T0.AddSeconds(10), width: 1000, start, end, now);

    [Test]
    public void Compute_ClosedInterval_HasCorrectXAndWidth()
    {
        var span = Compute(T0.AddSeconds(2), T0.AddSeconds(4), T0.AddSeconds(10));

        Assert.That(span, Is.Not.Null);
        Assert.That(span!.Value.X, Is.EqualTo(200).Within(1e-9));
        Assert.That(span.Value.Width, Is.EqualTo(200).Within(1e-9));
    }

    [Test]
    public void Compute_OpenInterval_ExtendsToNow()
    {
        var span = Compute(T0.AddSeconds(2), null, T0.AddSeconds(6));

        Assert.That(span!.Value.X, Is.EqualTo(200).Within(1e-9));
        Assert.That(span.Value.Width, Is.EqualTo(400).Within(1e-9));
    }

    [Test]
    public void Compute_IntervalEntirelyBeforeViewport_ReturnsNull()
    {
        Assert.That(Compute(T0.AddSeconds(-5), T0.AddSeconds(-1), T0.AddSeconds(10)), Is.Null);
    }

    [Test]
    public void Compute_IntervalEntirelyAfterViewport_ReturnsNull()
    {
        Assert.That(Compute(T0.AddSeconds(11), T0.AddSeconds(12), T0.AddSeconds(20)), Is.Null);
    }

    [Test]
    public void Compute_IntervalClippedToViewportBounds()
    {
        var span = Compute(T0.AddSeconds(-5), T0.AddSeconds(5), T0.AddSeconds(10));

        Assert.That(span!.Value.X, Is.EqualTo(0).Within(1e-9));
        Assert.That(span.Value.Width, Is.EqualTo(500).Within(1e-9));
    }

    [Test]
    public void Compute_FullDurationUsesUnclippedSpan()
    {
        var span = Compute(T0.AddSeconds(-5), T0.AddMilliseconds(20), T0.AddSeconds(10));

        Assert.That(span!.Value.FullDurationSeconds, Is.EqualTo(5.02).Within(1e-9));
    }

    [Test]
    public void Compute_ZeroSpanViewport_ReturnsNull()
    {
        var span = _geometry.Compute(T0, T0, width: 1000, T0, T0.AddSeconds(1), T0.AddSeconds(1));

        Assert.That(span, Is.Null);
    }

    [Test]
    public void TimeToX_MapsProportionally() =>
        Assert.That(_geometry.TimeToX(T0, T0.AddSeconds(10), 1000, T0.AddSeconds(3)), Is.EqualTo(300).Within(1e-9));

    [Test]
    public void TimeToX_ZeroSpan_ReturnsZero() =>
        Assert.That(_geometry.TimeToX(T0, T0, 1000, T0.AddSeconds(3)), Is.EqualTo(0));

    [Test]
    public void XToTime_IsTheInverseOfTimeToX()
    {
        var x = _geometry.TimeToX(T0, T0.AddSeconds(10), 1000, T0.AddSeconds(3.7));

        Assert.That(_geometry.XToTime(T0, T0.AddSeconds(10), 1000, x), Is.EqualTo(T0.AddSeconds(3.7)));
    }

    [Test]
    public void XToTime_ZeroWidth_ReturnsStart() =>
        Assert.That(_geometry.XToTime(T0, T0.AddSeconds(10), 0, 123), Is.EqualTo(T0));
}
