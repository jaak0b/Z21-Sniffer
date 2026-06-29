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
        var interval = new LocoInterval { MaxSpeed = maxSpeed, Start = T0 };
        foreach (var sample in samples) interval.Samples.Add(new LocoSpeedSample(T0.AddSeconds(sample.AtSeconds), sample.Speed, forward));
        return interval;
    }

    private LocoInterval MixedInterval(int maxSpeed, params (double AtSeconds, int Speed, bool Forward)[] samples)
    {
        var interval = new LocoInterval { MaxSpeed = maxSpeed, Start = T0 };
        foreach (var sample in samples) interval.Samples.Add(new LocoSpeedSample(T0.AddSeconds(sample.AtSeconds), sample.Speed, sample.Forward));
        return interval;
    }

    private void Draw(LocoInterval interval, bool showContent = true) =>
        _strategy.Draw(_source, interval, _surface, Rect, new BarContentContext(showContent, TimeSpan.FromSeconds(5)), Viewport);

    [Test]
    public void LaneHeight_ScalesFromBaseToDoubleWithZoom()
    {
        Assert.That(_strategy.LaneHeight(0), Is.EqualTo(34));
        Assert.That(_strategy.LaneHeight(0.5), Is.EqualTo(51));
        Assert.That(_strategy.LaneHeight(1), Is.EqualTo(68));
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
    public void Probe_AtHeldSample_ReportsSpeedAndDirection()
    {
        var interval = Interval(forward: true, maxSpeed: 100, (2, 80), (4, 90));

        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(3)), Does.Contain("Speed 80").And.Contain("forward"));
    }

    [Test]
    public void Probe_BeforeFirstSample_IsNull()
    {
        var interval = Interval(forward: true, maxSpeed: 100, (2, 80));

        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(1)), Is.Null);
    }

    [Test]
    public void Draw_MarksEachOnScreenSampleWithACircleCenteredOnItsPoint()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 80), (4, 90)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(_surface.Markers, Has.Count.EqualTo(2));
        Assert.That(_surface.Markers[0].CenterX, Is.EqualTo(points[0].X).Within(1e-6));
        Assert.That(_surface.Markers[0].CenterY, Is.EqualTo(points[0].Y).Within(1e-6));
        Assert.That(_surface.Markers[1].CenterX, Is.EqualTo(40).Within(1e-6));
    }

    [Test]
    public void Draw_Markers_UseTheSpeedLineInk()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 80)));

        var marker = _surface.Markers.Single();
        Assert.That(marker.Ink.Key, Is.EqualTo(TimelineInkKeys.LocoSpeedLine));
        Assert.That(marker.Radius, Is.GreaterThan(0));
    }

    [Test]
    public void Draw_Markers_FallOffSteeplyAsTheLaneShrinksToZeroWhenZoomedOut()
    {
        var interval = Interval(forward: true, maxSpeed: 100, (2, 80));
        var context = new BarContentContext(false, TimeSpan.FromSeconds(5));

        double Radius(double laneHeight)
        {
            _surface.Markers.Clear();
            _strategy.Draw(_source, interval, _surface, new BarRect(0, 0, 100, laneHeight), context, Viewport);
            return _surface.Markers.Single().Radius;
        }

        Assert.That(Radius(34), Is.EqualTo(0.0).Within(1e-6), "fully zoomed out — invisible");
        Assert.That(Radius(51), Is.EqualTo(0.625).Within(1e-6), "half zoom — already tiny");
        Assert.That(Radius(68), Is.EqualTo(2.5).Within(1e-6), "fully zoomed in — full size");
    }

    [Test]
    public void Draw_OffScreenReadings_DrawNoMarkers()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (-3, 20), (-2, 90), (-1, 30), (3, 80)));

        Assert.That(_surface.Markers, Has.Count.EqualTo(1));
    }

    [Test]
    public void Draw_ForwardOnly_DrawsAThinBaselineLineAcrossTheBarAtTheZeroPosition()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 50)));

        var line = _surface.Lines.Single(l => l.Ink.Key == TimelineInkKeys.LocoBaseline);
        Assert.That(line.StartX, Is.EqualTo(Rect.X));
        Assert.That(line.EndX, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(line.StartY, Is.EqualTo(49).Within(1e-6));
        Assert.That(line.EndY, Is.EqualTo(49).Within(1e-6));
        Assert.That(line.Thickness, Is.LessThan(2));
        Assert.That(line.Dashed, Is.True);
    }

    [Test]
    public void Draw_Bidirectional_BaselineLineRunsThroughTheCenter()
    {
        Draw(MixedInterval(maxSpeed: 100, (2, 50, true), (4, 50, false)));

        var line = _surface.Lines.Single(l => l.Ink.Key == TimelineInkKeys.LocoBaseline);
        Assert.That(line.StartY, Is.EqualTo(Rect.Y + Rect.H / 2).Within(1e-6));
        Assert.That(line.EndY, Is.EqualTo(Rect.Y + Rect.H / 2).Within(1e-6));
    }

    [Test]
    public void Draw_Bidirectional_CentersBaseline_ForwardAbove_ReverseBelow()
    {
        Draw(MixedInterval(maxSpeed: 100, (2, 100, true), (4, 100, false)));

        var forwardMarker = _surface.Markers[0];
        var reverseMarker = _surface.Markers[1];
        Assert.That(forwardMarker.CenterY, Is.EqualTo(3).Within(1e-6));
        Assert.That(reverseMarker.CenterY, Is.EqualTo(49).Within(1e-6));
    }

    [Test]
    public void Draw_Bidirectional_EqualSpeeds_AreEquidistantFromCenterBaseline()
    {
        Draw(MixedInterval(maxSpeed: 100, (2, 50, true), (4, 50, false)));

        var center = Rect.Y + Rect.H / 2;
        Assert.That(center - _surface.Markers[0].CenterY, Is.EqualTo(_surface.Markers[1].CenterY - center).Within(1e-6));
    }

    [Test]
    public void Draw_Reversal_StepRiserCrossesTheCenterBaseline()
    {
        Draw(MixedInterval(maxSpeed: 100, (2, 60, true), (4, 60, false)));

        var center = Rect.Y + Rect.H / 2;
        var points = _surface.Polylines.Single().Points;
        var riser = Enumerable.Range(1, points.Count - 1)
            .Any(i => Math.Abs(points[i].X - points[i - 1].X) < 1e-6
                && Math.Min(points[i].Y, points[i - 1].Y) < center
                && Math.Max(points[i].Y, points[i - 1].Y) > center);
        Assert.That(riser, Is.True);
    }

    [Test]
    public void Probe_ReportsEachSamplesOwnDirection()
    {
        var interval = MixedInterval(maxSpeed: 100, (2, 80, true), (4, 90, false));

        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(2)), Does.Contain("forward"));
        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(4)), Does.Contain("backward"));
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
    public void Probe_Reverse_ReportsBackwardDirection()
    {
        var interval = Interval(forward: false, maxSpeed: 100, (2, 80));

        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(2)), Does.Contain("Speed 80").And.Contain("backward"));
    }

    [Test]
    public void Draw_PlotsEverySampleAsAPolylineVertex()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (1, 30), (3, 60), (5, 90)));

        var points = _surface.Polylines.Single().Points;
        foreach (var marker in _surface.Markers)
            Assert.That(points, Has.Some.Matches<PlotPoint>(p =>
                Math.Abs(p.X - marker.CenterX) < 1e-6 && Math.Abs(p.Y - marker.CenterY) < 1e-6));
        Assert.That(_surface.Markers, Has.Count.EqualTo(3));
    }

    [Test]
    public void Draw_SteppedPath_HasOnlyAxisAlignedSegments()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (1, 30), (3, 60), (5, 90)));

        var points = _surface.Polylines.Single().Points;
        for (var index = 1; index < points.Count; index++)
        {
            var sameX = Math.Abs(points[index].X - points[index - 1].X) < 1e-6;
            var sameY = Math.Abs(points[index].Y - points[index - 1].Y) < 1e-6;
            Assert.That(sameX || sameY, Is.True, $"segment {index} is diagonal");
        }
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
        var interval = new LocoInterval { MaxSpeed = 100, Start = T0 };

        Assert.That(() => Draw(interval), Throws.Nothing);
        Assert.That(_surface.Polylines.Single().Points, Is.Empty);
    }

    [Test]
    public void Draw_SingleSample_StillDrawsAHoldLineToBarEnd()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 50)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(points.Last().X, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(points.Select(p => p.Y), Is.All.EqualTo(points[0].Y));
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
        Assert.That(_surface.Markers, Has.Count.EqualTo(2));
    }

    [Test]
    public void Draw_ReadingExactlyAtRightEdge_StaysOnScreen()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (10, 50)));

        Assert.That(_surface.Markers, Has.Count.EqualTo(1));
    }

    [Test]
    public void Draw_IntervalSpansEntireViewport_DrawsFlatLineAtHeldSpeed()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (-2, 40), (20, 80)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(points[0].X, Is.EqualTo(Rect.X));
        Assert.That(points.Last().X, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(points.Select(p => p.Y), Is.All.EqualTo(30.6).Within(1e-6));
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
    public void Draw_WithoutContent_DrawsNoLabel()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 80)), showContent: false);

        Assert.That(_surface.Texts.Where(t => t.Ink.Key == TimelineInkKeys.LocoText), Is.Empty);
    }

    [Test]
    public void Draw_Label_NeverContainsTheSpeed()
    {
        Draw(Interval(forward: true, maxSpeed: 100, (2, 40), (4, 90)));

        var label = _surface.Texts.Single(t => t.Ink.Key == TimelineInkKeys.LocoText);
        Assert.That(label.Text, Does.Not.Contain("90"));
    }

}
