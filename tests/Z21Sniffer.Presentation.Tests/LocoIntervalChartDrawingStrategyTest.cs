using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LocoIntervalChartDrawingStrategyTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    private static readonly BarRect Rect = new(0, 0, 100, 52);
    private static readonly ChartViewport Viewport = new(T0, T0.AddSeconds(10), 100);

    private LocoIntervalChartDrawingStrategy _strategy = null!;
    private LocoIntervalSource _source = null!;
    private RecordingTimelineSurface _surface = null!;

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _strategy = new LocoIntervalChartDrawingStrategy();
        _source = new LocoIntervalSource { Id = "loco:3", Address = 3 };
        _surface = new RecordingTimelineSurface();
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private LocoInterval Interval(bool forward, int maxSpeed, params (double AtSeconds, int Speed)[] samples)
    {
        var interval = new LocoInterval { Forward = forward, MaxSpeed = maxSpeed, Start = T0 };
        foreach (var (at, speed) in samples) interval.Samples.Add(new LocoSpeedSample(T0.AddSeconds(at), speed));
        return interval;
    }

    private void Draw(LocoInterval interval, bool showContent = true) =>
        _strategy.Draw(_source, interval, _surface, Rect, new BarContentContext(showContent, false, TimeSpan.FromSeconds(5)), Viewport);

    [Test]
    public void LaneHeight_ScalesFromBaseToDoubleWithZoom()
    {
        Assert.That(_strategy.LaneHeight(0), Is.EqualTo(26));
        Assert.That(_strategy.LaneHeight(0.5), Is.EqualTo(39));
        Assert.That(_strategy.LaneHeight(1), Is.EqualTo(52));
    }

    [Test]
    public void Draw_FillsLocoBar()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 50)));

        Assert.That(_surface.Fills, Has.Some.Matches<RecordingTimelineSurface.FillOp>(f => f.Ink.Key == TimelineInkKeys.LocoBar));
    }

    [Test]
    public void Draw_Forward_MaxSpeedPlotsAtTop()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 100)));

        var point = _surface.Polylines.Single().Points[0];
        Assert.That(point.Y, Is.EqualTo(3).Within(1e-6));
    }

    [Test]
    public void Draw_Reverse_MaxSpeedPlotsAtBottom()
    {
        Draw(Interval(forward: false, maxSpeed: 100, (2, 100)));

        var point = _surface.Polylines.Single().Points[0];
        Assert.That(point.Y, Is.EqualTo(49).Within(1e-6));
    }

    [Test]
    public void Draw_Forward_ScalesSpeedAgainstMax()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 50)));

        var point = _surface.Polylines.Single().Points[0];
        Assert.That(point.Y, Is.EqualTo(26).Within(1e-6));
    }

    [Test]
    public void Draw_PlotsSampleAtItsTimePosition()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (5, 50)));

        var point = _surface.Polylines.Single().Points[0];
        Assert.That(point.X, Is.EqualTo(50).Within(1e-6));
    }

    [Test]
    public void Draw_WithContent_RegistersHitAreaPerSampleWithSpeedDirectionAndTime()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 80), (4, 90)));

        Assert.That(_surface.Hits, Has.Count.EqualTo(2));
        Assert.That(_surface.Hits[0].Text, Does.Contain("Speed 80").And.Contain("forward"));
    }

    [Test]
    public void Draw_WithoutContent_RegistersNoHitAreas()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 80)), showContent: false);

        Assert.That(_surface.Hits, Is.Empty);
    }

    [Test]
    public void Draw_Stopped_DrawsStoppedFlagAtTrailingEdgeWithFixedWidth()
    {
        var interval = Interval(forward: true, maxSpeed: 100, (2, 50));
        interval.End = T0.AddSeconds(6);
        interval.EndReason = IntervalEndReason.Stopped;

        Draw(interval);

        var flag = _surface.Fills.Single(f => f.Ink.Key == TimelineInkKeys.StoppedFlag);
        Assert.That(flag.Rect.W, Is.EqualTo(4));
        Assert.That(flag.Rect.X, Is.EqualTo(Rect.X + Rect.W - 4));
    }

    [Test]
    public void Draw_Reverse_TooltipReportsBackwardDirection()
    {
        Draw(Interval(forward: false, maxSpeed: 100, (2, 80)));

        Assert.That(_surface.Hits[0].Text, Does.Contain("Speed 80").And.Contain("backward"));
    }

    [Test]
    public void Draw_HitArea_IsCenteredOnTheSamplePoint()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (5, 50)));

        var point = _surface.Polylines.Single().Points[0];
        var hit = _surface.Hits.Single();
        Assert.That(hit.Rect.X, Is.EqualTo(point.X - 4));
        Assert.That(hit.Rect.Y, Is.EqualTo(point.Y - 4));
        Assert.That(hit.Rect.W, Is.EqualTo(8));
        Assert.That(hit.Rect.H, Is.EqualTo(8));
    }

    [Test]
    public void Draw_PlotsEverySampleAsAPolylinePoint()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (1, 30), (3, 60), (5, 90)));

        Assert.That(_surface.Polylines.Single().Points, Has.Count.EqualTo(4));
        Assert.That(_surface.Hits, Has.Count.EqualTo(3));
    }

    [Test]
    public void Draw_HoldsLastSpeedFlatToTheBarEnd()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (1, 30), (3, 60)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(points.Last().X, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(points.Last().Y, Is.EqualTo(points[^2].Y));
    }

    [Test]
    public void Draw_IntervalWithNoSamples_DrawsNoLineAndDoesNotThrow()
    {
        var interval = new LocoInterval { Forward = true, MaxSpeed = 100, Start = T0 };

        Assert.That(() => Draw(interval), Throws.Nothing);
        Assert.That(_surface.Polylines.Single().Points, Is.Empty);
    }

    [Test]
    public void Draw_SingleSample_StillDrawsAHoldLineToBarEnd()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 50)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(points, Has.Count.EqualTo(2));
        Assert.That(points[1].X, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(points[1].Y, Is.EqualTo(points[0].Y));
    }

    [Test]
    public void Draw_ZeroMaxSpeed_PlotsAtBaseline()
    {
        Draw(Interval(forward: true, maxSpeed: 0, (2, 50)));

        Assert.That(_surface.Polylines.Single().Points[0].Y, Is.EqualTo(49).Within(1e-6));
    }

    [Test]
    public void Draw_IntervalStartsBeforeViewport_EntersAtLastOffScreenReadingWithoutBunching()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (-3, 20), (-2, 90), (-1, 30), (3, 80)));

        var atLeftEdge = _surface.Polylines.Single().Points.Where(point => point.X == Rect.X).ToList();
        Assert.That(atLeftEdge, Has.Count.EqualTo(1));
        Assert.That(atLeftEdge[0].Y, Is.EqualTo(35.2).Within(1e-6));
    }

    [Test]
    public void Draw_ReadingExactlyAtLeftEdge_CountsOnceAndStaysOnScreen()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (0, 50), (3, 80)));

        var atLeftEdge = _surface.Polylines.Single().Points.Where(point => point.X == Rect.X).ToList();
        Assert.That(atLeftEdge, Has.Count.EqualTo(1));
        Assert.That(_surface.Hits, Has.Count.EqualTo(2));
    }

    [Test]
    public void Draw_ReadingExactlyAtRightEdge_StaysOnScreen()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (10, 50)));

        Assert.That(_surface.Hits, Has.Count.EqualTo(1));
    }

    [Test]
    public void Draw_OffScreenReadings_RegisterNoHitAreas()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (-3, 20), (-2, 90), (-1, 30), (3, 80)));

        Assert.That(_surface.Hits, Has.Count.EqualTo(1));
        Assert.That(_surface.Hits[0].Text, Does.Contain("80"));
    }

    [Test]
    public void Draw_IntervalSpansEntireViewport_DrawsFlatLineAtHeldSpeed()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (-2, 40), (20, 80)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(points, Has.Count.EqualTo(2));
        Assert.That(points[0].X, Is.EqualTo(Rect.X));
        Assert.That(points[1].X, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(points[0].Y, Is.EqualTo(30.6).Within(1e-6));
        Assert.That(points[1].Y, Is.EqualTo(30.6).Within(1e-6));
    }

    [Test]
    public void Draw_Label_WithCustomAlias_ShowsAliasAndAddressWithoutSpeed()
    {
        _source.Label = "Express";

        Draw(Interval(forward: true, maxSpeed: 100, (2, 40), (4, 90)));

        var label = _surface.Texts.Single(t => t.Ink.Key == TimelineInkKeys.LocoText);
        Assert.That(label.Text, Is.EqualTo("Express · Loco 3"));
        Assert.That(label.X, Is.EqualTo(Rect.X + 5));
        Assert.That(label.Y, Is.EqualTo(Rect.Y + 3 + 6));
    }

    [Test]
    public void Draw_Label_WithoutCustomAlias_ShowsLabelledAddressOnly()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 40), (4, 90)));

        var label = _surface.Texts.Single(t => t.Ink.Key == TimelineInkKeys.LocoText);
        Assert.That(label.Text, Is.EqualTo("Loco 3"));
    }

    [Test]
    public void Draw_Label_WithBlankAlias_FallsBackToLabelledAddressWithoutDanglingSeparator()
    {
        _source.Label = "   ";

        Draw(Interval(forward: true, maxSpeed: 100, (2, 40)));

        var label = _surface.Texts.Single(t => t.Ink.Key == TimelineInkKeys.LocoText);
        Assert.That(label.Text, Is.EqualTo("Loco 3"));
    }

    [Test]
    public void Draw_Label_NeverContainsTheSpeed()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 40), (4, 90)));

        var label = _surface.Texts.Single(t => t.Ink.Key == TimelineInkKeys.LocoText);
        Assert.That(label.Text, Does.Not.Contain("90"));
    }

}
