using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TimelineLayoutTest
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
    private readonly TimelineLayout _layout = new();

    private static TimelineViewport Viewport() =>
        new(T0, T0.AddSeconds(10), Width: 1000, Height: 200, RowHeight: 20);

    private static readonly SensorKey SensorA = new(1, 1);
    private static readonly SensorKey SensorB = new(1, 2);

    [Test]
    public void TimeToX_StartMapsToZero() =>
        Assert.That(_layout.TimeToX(Viewport(), T0), Is.EqualTo(0).Within(1e-9));

    [Test]
    public void TimeToX_EndMapsToWidth() =>
        Assert.That(_layout.TimeToX(Viewport(), T0.AddSeconds(10)), Is.EqualTo(1000).Within(1e-9));

    [Test]
    public void TimeToX_MidpointMapsToHalfWidth() =>
        Assert.That(_layout.TimeToX(Viewport(), T0.AddSeconds(5)), Is.EqualTo(500).Within(1e-9));

    [Test]
    public void TimeToX_ZeroSpanViewport_ReturnsZero()
    {
        var degenerate = Viewport() with { End = T0 };
        Assert.That(_layout.TimeToX(degenerate, T0.AddSeconds(5)), Is.EqualTo(0));
    }

    [Test]
    public void Bars_ClosedInterval_HasCorrectXAndWidth()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(2), T0.AddSeconds(4)) };

        var bar = _layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10)).Single();

        Assert.That(bar.X, Is.EqualTo(200).Within(1e-9));
        Assert.That(bar.Width, Is.EqualTo(200).Within(1e-9));
    }

    [Test]
    public void Bars_OpenInterval_ExtendsToNow()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(2), null) };

        var bar = _layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(6)).Single();

        Assert.That(bar.X, Is.EqualTo(200).Within(1e-9));
        Assert.That(bar.Width, Is.EqualTo(400).Within(1e-9));
    }

    [Test]
    public void Bars_RowIndexDeterminesY()
    {
        var intervals = new[] { new SensorInterval(SensorB, T0.AddSeconds(1), T0.AddSeconds(2)) };

        var bar = _layout.Bars(Viewport(), [SensorA, SensorB], intervals, T0.AddSeconds(10)).Single();

        Assert.That(bar.RowIndex, Is.EqualTo(1));
        Assert.That(bar.Y, Is.EqualTo(20).Within(1e-9));
        Assert.That(bar.Height, Is.EqualTo(20).Within(1e-9));
    }

    [Test]
    public void Bars_IntervalEntirelyBeforeViewport_Skipped()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(-5), T0.AddSeconds(-1)) };

        Assert.That(_layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10)), Is.Empty);
    }

    [Test]
    public void Bars_IntervalClippedToViewportBounds()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(-5), T0.AddSeconds(5)) };

        var bar = _layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10)).Single();

        Assert.That(bar.X, Is.EqualTo(0).Within(1e-9));
        Assert.That(bar.Width, Is.EqualTo(500).Within(1e-9));
    }

    [Test]
    public void Bars_SensorNotInRows_Skipped()
    {
        var intervals = new[] { new SensorInterval(SensorB, T0.AddSeconds(1), T0.AddSeconds(2)) };

        Assert.That(_layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10)), Is.Empty);
    }

    [Test]
    public void Bars_ShortInterval_HighlightedWhenUnderThreshold()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(2), T0.AddSeconds(2).AddMilliseconds(40)) };

        var bar = _layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10), highlightUnderSeconds: 1.0).Single();

        Assert.That(bar.Highlighted, Is.True);
    }

    [Test]
    public void Bars_LongInterval_NotHighlighted()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(2), T0.AddSeconds(6)) };

        var bar = _layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10), highlightUnderSeconds: 1.0).Single();

        Assert.That(bar.Highlighted, Is.False);
    }

    [Test]
    public void Bars_NoThreshold_NeverHighlighted()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(2), T0.AddSeconds(2).AddMilliseconds(10)) };

        var bar = _layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10), highlightUnderSeconds: null).Single();

        Assert.That(bar.Highlighted, Is.False);
    }

    [Test]
    public void Bars_HighlightUsesFullDurationNotClippedWidth()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0.AddSeconds(-5), T0.AddMilliseconds(20)) };

        var bar = _layout.Bars(Viewport(), [SensorA], intervals, T0.AddSeconds(10), highlightUnderSeconds: 1.0).Single();

        Assert.That(bar.Highlighted, Is.False);
    }

    [Test]
    public void Bars_VerticalBand_SkipsRowsOutsideIt()
    {
        var intervals = new[]
        {
            new SensorInterval(SensorA, T0.AddSeconds(1), T0.AddSeconds(2)),
            new SensorInterval(SensorB, T0.AddSeconds(1), T0.AddSeconds(2)),
            new SensorInterval(new SensorKey(1, 3), T0.AddSeconds(1), T0.AddSeconds(2))
        };
        var rows = new[] { SensorA, SensorB, new SensorKey(1, 3) };

        var topBand = _layout.Bars(Viewport(), rows, intervals, T0.AddSeconds(10),
            verticalOffset: 0, visibleHeight: 40);
        var lowerBand = _layout.Bars(Viewport(), rows, intervals, T0.AddSeconds(10),
            verticalOffset: 40, visibleHeight: 40);

        Assert.That(topBand.Select(b => b.RowIndex), Is.EqualTo(new[] { 0, 1 }));
        Assert.That(lowerBand.Select(b => b.RowIndex), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void Ticks_ZeroStep_ReturnsEmpty()
    {
        Assert.That(_layout.Ticks(Viewport(), TimeSpan.Zero), Is.Empty);
    }

    [Test]
    public void Ticks_GeneratesEvenlySpacedTicksAcrossViewport()
    {
        var ticks = _layout.Ticks(Viewport(), TimeSpan.FromSeconds(5));

        Assert.That(ticks.Select(t => t.X), Is.EqualTo(new[] { 0d, 500d, 1000d }));
    }
}
