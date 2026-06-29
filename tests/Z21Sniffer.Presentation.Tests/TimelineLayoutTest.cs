using NUnit.Framework;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TimelineLayoutTest
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
    private readonly TimelineLayout _layout = new();

    private static TimelineViewport Viewport() =>
        new(T0, T0.AddSeconds(10), Width: 1000, Height: 200, RowHeight: 20);

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
    public void XToTime_IsTheInverseOfTimeToX()
    {
        var time = T0.AddSeconds(3.7);
        var x = _layout.TimeToX(Viewport(), time);

        Assert.That(_layout.XToTime(Viewport(), x), Is.EqualTo(time));
    }

    [Test]
    public void XToTime_MidWidthMapsToMidViewport() =>
        Assert.That(_layout.XToTime(Viewport(), 500), Is.EqualTo(T0.AddSeconds(5)));

    [Test]
    public void XToTime_ZeroWidthViewport_ReturnsStart()
    {
        var degenerate = Viewport() with { Width = 0 };
        Assert.That(_layout.XToTime(degenerate, 123), Is.EqualTo(T0));
    }

    [Test]
    public void XToTime_NegativeWidthViewport_ReturnsStart()
    {
        var degenerate = Viewport() with { Width = -5 };
        Assert.That(_layout.XToTime(degenerate, 123), Is.EqualTo(T0));
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

    [Test]
    public void RangesOverlap_Overlapping_IsTrue() =>
        Assert.That(_layout.RangesOverlap(100, 160, 150, 210), Is.True);

    [Test]
    public void RangesOverlap_OverlapFromTheLeft_IsTrue() =>
        Assert.That(_layout.RangesOverlap(150, 210, 100, 160), Is.True);

    [Test]
    public void RangesOverlap_Disjoint_IsFalse() =>
        Assert.That(_layout.RangesOverlap(100, 160, 200, 260), Is.False);

    [Test]
    public void RangesOverlap_JustTouching_IsFalse() =>
        Assert.That(_layout.RangesOverlap(100, 160, 160, 220), Is.False);

    [Test]
    public void RangesOverlap_JustTouchingFromTheLeft_IsFalse() =>
        Assert.That(_layout.RangesOverlap(160, 220, 100, 160), Is.False);

    [Test]
    public void CenteredLabelX_CentersTheLabelOnTheCursor() =>
        Assert.That(_layout.CenteredLabelX(cursorX: 100, labelWidth: 40, viewportWidth: 800), Is.EqualTo(80));

    [Test]
    public void CenteredLabelX_NearLeftEdge_ClampsToZero() =>
        Assert.That(_layout.CenteredLabelX(cursorX: 5, labelWidth: 40, viewportWidth: 800), Is.EqualTo(0));

    [Test]
    public void CenteredLabelX_NearRightEdge_ClampsSoLabelStaysVisible() =>
        Assert.That(_layout.CenteredLabelX(cursorX: 795, labelWidth: 40, viewportWidth: 800), Is.EqualTo(760));

    [Test]
    public void CenteredLabelX_LabelWiderThanViewport_ClampsToZero() =>
        Assert.That(_layout.CenteredLabelX(cursorX: 100, labelWidth: 900, viewportWidth: 800), Is.EqualTo(0));
}
