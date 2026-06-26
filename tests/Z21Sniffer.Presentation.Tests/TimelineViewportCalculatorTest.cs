using NUnit.Framework;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TimelineViewportCalculatorTest
{
    private static readonly DateTimeOffset Earliest = new(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = Earliest.AddMinutes(10);
    private readonly TimelineViewportCalculator _calc = new();

    private static TimelineWindow Window(double endOffsetSeconds, double durationSeconds) =>
        new(Earliest.AddSeconds(endOffsetSeconds), TimeSpan.FromSeconds(durationSeconds));

    [Test]
    public void Clamp_EndPastNow_ClampsToNow()
    {
        var result = _calc.Clamp(new TimelineWindow(Now.AddMinutes(5), TimeSpan.FromSeconds(60)), Earliest, Now);

        Assert.That(result.End, Is.EqualTo(Now));
    }

    [Test]
    public void Clamp_StartBeforeEarliest_ClampsEndSoStartIsEarliest()
    {
        var result = _calc.Clamp(new TimelineWindow(Earliest.AddSeconds(10), TimeSpan.FromSeconds(60)), Earliest, Now);

        Assert.That(result.Start, Is.EqualTo(Earliest));
        Assert.That(result.End, Is.EqualTo(Earliest.AddSeconds(60)));
    }

    [Test]
    public void Clamp_DurationBelowMin_ClampsToMin()
    {
        var result = _calc.Clamp(new TimelineWindow(Now, TimeSpan.FromMilliseconds(100)), Earliest, Now);

        Assert.That(result.Duration, Is.EqualTo(_calc.MinDuration));
    }

    [Test]
    public void Clamp_DurationBeyondHistory_CappedToHistorySpan()
    {
        var result = _calc.Clamp(new TimelineWindow(Now, TimeSpan.FromHours(2)), Earliest, Now);

        Assert.That(result.Duration, Is.EqualTo(Now - Earliest));
        Assert.That(result.Start, Is.EqualTo(Earliest));
    }

    [Test]
    public void Pan_ShiftsEndBackByDelta()
    {
        var result = _calc.Pan(Window(endOffsetSeconds: 300, durationSeconds: 60), TimeSpan.FromSeconds(-100), Earliest, Now);

        Assert.That(result.End, Is.EqualTo(Earliest.AddSeconds(200)));
    }

    [Test]
    public void Pan_PastNow_ClampsToNow()
    {
        var result = _calc.Pan(Window(endOffsetSeconds: 590, durationSeconds: 60), TimeSpan.FromSeconds(120), Earliest, Now);

        Assert.That(result.End, Is.EqualTo(Now));
    }

    [Test]
    public void Zoom_In_ReducesDuration()
    {
        var result = _calc.Zoom(Window(endOffsetSeconds: 300, durationSeconds: 60), factor: 0.5, anchorFraction: 0.5, Earliest, Now);

        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public void Zoom_AnchorRightEdge_KeepsEnd()
    {
        var window = Window(endOffsetSeconds: 300, durationSeconds: 60);

        var result = _calc.Zoom(window, factor: 0.5, anchorFraction: 1.0, Earliest, Now);

        Assert.That(result.End, Is.EqualTo(window.End));
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public void ScrollSeconds_IsStartOffsetFromEarliest()
    {
        var result = _calc.ScrollSeconds(Window(endOffsetSeconds: 200, durationSeconds: 60), Earliest, Now);

        Assert.That(result, Is.EqualTo(140).Within(1e-9));
    }

    [Test]
    public void MaxScrollSeconds_IsTotalMinusWindow()
    {
        var result = _calc.MaxScrollSeconds(Window(endOffsetSeconds: 200, durationSeconds: 60), Earliest, Now);

        Assert.That(result, Is.EqualTo(600 - 60).Within(1e-9));
    }

    [Test]
    public void WithStartOffset_PlacesWindowStartAtOffset()
    {
        var result = _calc.WithStartOffset(Window(endOffsetSeconds: 600, durationSeconds: 60), offsetSeconds: 100, Earliest, Now);

        Assert.That(result.Start, Is.EqualTo(Earliest.AddSeconds(100)));
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromSeconds(60)));
    }
}
