using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class SystemCurrentIntervalChartDrawingStrategyTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    private static readonly BarRect Rect = new(0, 0, 100, 52);
    private static readonly ChartViewport Viewport = new(T0, T0.AddSeconds(10), 100);

    private SystemCurrentIntervalChartDrawingStrategy _strategy = null!;
    private SystemCurrentSource _source = null!;
    private RecordingTimelineSurface _surface = null!;

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _strategy = new SystemCurrentIntervalChartDrawingStrategy();
        _source = new SystemCurrentSource { Id = "systemcurrent" };
        _surface = new RecordingTimelineSurface();
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private SystemCurrentInterval Interval(int maxCurrent, params (double AtSeconds, int Milliamps)[] samples) =>
        Build(typeCode: 513, deviceName: "Z21 (black)", maxCurrent, samples);

    private SystemCurrentInterval UnknownInterval(params (double AtSeconds, int Milliamps)[] samples) =>
        Build(typeCode: 0, deviceName: null, maxCurrentMilliamps: null, samples);

    private SystemCurrentInterval Build(int typeCode, string? deviceName, int? maxCurrentMilliamps, (double AtSeconds, int Milliamps)[] samples)
    {
        var interval = new SystemCurrentInterval { TypeCode = typeCode, DeviceName = deviceName, MaxCurrentMilliamps = maxCurrentMilliamps, Start = T0 };
        foreach (var sample in samples) interval.Samples.Add(new SystemCurrentSample(T0.AddSeconds(sample.AtSeconds), sample.Milliamps));
        return interval;
    }

    private void Draw(SystemCurrentInterval interval, bool showContent = true) =>
        _strategy.Draw(_source, interval, _surface, Rect, new BarContentContext(showContent, TimeSpan.FromSeconds(5)), Viewport);

    [Test]
    public void LaneHeight_ScalesFromBaseToDoubleWithZoom()
    {
        Assert.That(_strategy.LaneHeight(0), Is.EqualTo(34));
        Assert.That(_strategy.LaneHeight(1), Is.EqualTo(68));
    }

    [Test]
    public void Draw_FillsTheCurrentBar()
    {
        Draw(Interval(3200, (2, 800)));

        Assert.That(_surface.Fills, Has.Some.Matches<RecordingTimelineSurface.FillOp>(f => f.Ink.Key == TimelineInkKeys.SystemCurrentBar));
    }

    [Test]
    public void Draw_MaxCurrentPlotsAtTop()
    {
        Draw(Interval(maxCurrent: 1000, (2, 1000)));

        Assert.That(_surface.Polylines.Single().Points[0].Y, Is.EqualTo(3).Within(1e-6));
    }

    [Test]
    public void Draw_ZeroCurrentPlotsAtBottomBaseline()
    {
        Draw(Interval(maxCurrent: 1000, (2, 0)));

        Assert.That(_surface.Polylines.Single().Points[0].Y, Is.EqualTo(49).Within(1e-6));
    }

    [Test]
    public void Draw_HalfCurrentPlotsAtMidHeight()
    {
        Draw(Interval(maxCurrent: 1000, (2, 500)));

        Assert.That(_surface.Polylines.Single().Points[0].Y, Is.EqualTo(26).Within(1e-6));
    }

    [Test]
    public void Draw_PlotsSampleAtItsTimePosition()
    {
        Draw(Interval(1000, (5, 500)));

        Assert.That(_surface.Polylines.Single().Points[0].X, Is.EqualTo(50).Within(1e-6));
    }

    [Test]
    public void Draw_BetweenSamples_IsADiagonalLineNotStepped()
    {
        Draw(Interval(maxCurrent: 1000, (2, 200), (6, 600)));

        var points = _surface.Polylines.Single().Points;
        var onScreen = points.Where(p => p.X > 20 - 1e-6 && p.X < 60 + 1e-6).ToList();
        Assert.That(onScreen[0].X, Is.Not.EqualTo(onScreen[1].X));
        Assert.That(onScreen[0].Y, Is.Not.EqualTo(onScreen[1].Y));
    }

    [Test]
    public void Draw_DrawsADashedBaselineAcrossTheBottom()
    {
        Draw(Interval(1000, (2, 500)));

        var line = _surface.Lines.Single(l => l.Ink.Key == TimelineInkKeys.SystemCurrentBaseline);
        Assert.That(line.StartX, Is.EqualTo(Rect.X));
        Assert.That(line.StartY, Is.EqualTo(49).Within(1e-6));
        Assert.That(line.EndX, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(line.EndY, Is.EqualTo(49).Within(1e-6));
        Assert.That(line.Dashed, Is.True);
    }

    [Test]
    public void Draw_MarksEachOnScreenSample()
    {
        Draw(Interval(1000, (2, 200), (4, 400)));

        Assert.That(_surface.Markers, Has.Count.EqualTo(2));
    }

    [Test]
    public void Draw_ReadingExactlyAtTheRightEdge_IsMarked()
    {
        Draw(Interval(1000, (10, 500)));

        Assert.That(_surface.Markers, Has.Count.EqualTo(1));
    }

    [Test]
    public void Draw_ReadingExactlyAtTheLeftEdge_IsMarkedOnceWithoutADuplicateEntryVertex()
    {
        Draw(Interval(1000, (0, 500)));

        Assert.That(_surface.Markers, Has.Count.EqualTo(1));
        Assert.That(_surface.Polylines.Single().Points, Has.Count.EqualTo(2));
    }

    [Test]
    public void Draw_ReadingBeforeTheViewport_EntersAtTheLeftEdgeAndIsNotMarked()
    {
        Draw(Interval(maxCurrent: 1000, (-2, 200), (3, 600)));

        Assert.That(_surface.Markers, Has.Count.EqualTo(1));
        Assert.That(_surface.Polylines.Single().Points[0].X, Is.EqualTo(Rect.X).Within(1e-6));
    }

    [Test]
    public void Draw_NoSamples_DrawsAnEmptyPolylineWithoutThrowing()
    {
        var interval = new SystemCurrentInterval { MaxCurrentMilliamps = 1000, Start = T0 };

        Assert.That(() => Draw(interval), Throws.Nothing);
        Assert.That(_surface.Polylines.Single().Points, Is.Empty);
        Assert.That(_surface.Markers, Is.Empty);
    }

    [Test]
    public void Draw_WithoutContent_DrawsNoLabel()
    {
        Draw(Interval(1000, (2, 500)), showContent: false);

        Assert.That(_surface.Texts.Where(t => t.Ink.Key == TimelineInkKeys.SystemCurrentText), Is.Empty);
    }

    [Test]
    public void Draw_HoldsLastReadingFlatToTheBarEnd()
    {
        Draw(Interval(maxCurrent: 1000, (2, 300)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(points.Last().X, Is.EqualTo(Rect.X + Rect.W));
        Assert.That(points.Last().Y, Is.EqualTo(points[0].Y).Within(1e-6));
    }

    [Test]
    public void Draw_WithContent_LabelsTheRow()
    {
        Draw(Interval(1000, (2, 500)));

        var label = _surface.Texts.Single(t => t.Ink.Key == TimelineInkKeys.SystemCurrentText);
        Assert.That(label.Text, Is.EqualTo("System current"));
        Assert.That(label.X, Is.EqualTo(Rect.X + 5));
        Assert.That(label.Y, Is.EqualTo(Rect.Y + 3 + 6));
    }

    [Test]
    public void Draw_Stopped_DrawsStoppedFlagAtTrailingEdgeWithFixedWidth()
    {
        var interval = Interval(1000, (2, 500));
        interval.End = T0.AddSeconds(6);
        interval.EndReason = IntervalEndReason.Stopped;

        Draw(interval);

        var flag = _surface.Fills.Single(f => f.Ink.Key == TimelineInkKeys.StoppedFlag);
        Assert.That(flag.Rect.W, Is.EqualTo(4));
        Assert.That(flag.Rect.X, Is.EqualTo(Rect.X + Rect.W - 4));
    }

    [Test]
    public void Probe_ForAKnownDevice_ShowsNameInterpolatedCurrentAndMax()
    {
        var interval = Build(typeCode: 529, deviceName: "Z21 XL", maxCurrentMilliamps: 6000, new[] { (0d, 800), (10d, 1000) });

        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(5)), Is.EqualTo("Z21 XL: 900 / 6000 mA"));
    }

    [Test]
    public void Probe_ForAnUnknownDevice_ShowsOnlyTheCurrent()
    {
        var interval = UnknownInterval((0, 800), (10, 1000));

        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(5)), Is.EqualTo("900 mA"));
    }

    [Test]
    public void Probe_BeforeFirstSample_IsNull()
    {
        var interval = Interval(3200, (4, 800));

        Assert.That(_strategy.Probe(_source, interval, T0.AddSeconds(1)), Is.Null);
    }

    [Test]
    public void Draw_ZeroMaxCurrent_PlotsAtBaselineWithoutThrowing()
    {
        Assert.That(() => Draw(Interval(maxCurrent: 0, (2, 500))), Throws.Nothing);
        Assert.That(_surface.Polylines.Single().Points[0].Y, Is.EqualTo(49).Within(1e-6));
    }

    [Test]
    public void Draw_UnknownDevice_AutoScalesToTheIntervalsOwnPeak()
    {
        Draw(UnknownInterval((2, 500), (4, 1000)));

        var points = _surface.Polylines.Single().Points;
        Assert.That(points[0].Y, Is.EqualTo(26).Within(1e-6), "500 of a 1000 peak sits mid-row");
        Assert.That(points[1].Y, Is.EqualTo(3).Within(1e-6), "the peak 1000 reaches the top");
    }

    [Test]
    public void Draw_UnknownDeviceWithNoSamples_DoesNotThrow()
    {
        Assert.That(() => Draw(UnknownInterval()), Throws.Nothing);
        Assert.That(_surface.Polylines.Single().Points, Is.Empty);
    }
}
