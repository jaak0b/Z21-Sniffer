using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class SensorIntervalChartDrawingStrategyTest
{
    private static readonly SensorKey Sensor = new(1, 1);
    private static readonly BarRect Rect = new(10, 20, 300, 26);

    private SensorIntervalChartDrawingStrategy _strategy = null!;
    private FeedbackSensorSource _source = null!;
    private FeedbackSensorInterval _interval = null!;
    private RecordingTimelineSurface _surface = null!;

    [SetUp]
    public void SetUp()
    {
        _strategy = new SensorIntervalChartDrawingStrategy();
        _source = new FeedbackSensorSource { Sensor = Sensor, Label = "M1.1" };
        _interval = new FeedbackSensorInterval { Sensor = Sensor, Start = DateTimeOffset.UnixEpoch };
        _surface = new RecordingTimelineSurface();
    }

    private void Draw(BarContentContext ctx) => _strategy.Draw(_source, _interval, _surface, Rect, ctx);

    [Test]
    public void Draw_NotHighlighted_FillsBarInk()
    {
        Draw(new BarContentContext(ShowContent: false, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Fills, Has.Exactly(1).Matches<RecordingTimelineSurface.FillOp>(
            f => f.Ink.Key == TimelineInkKeys.Bar && f.Rect == Rect));
        Assert.That(_surface.Strokes, Is.Empty);
    }

    [Test]
    public void Draw_Highlighted_FillsHighlightedInkAndStrokesOutline()
    {
        Draw(new BarContentContext(ShowContent: false, Highlighted: true, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Fills, Has.Exactly(1).Matches<RecordingTimelineSurface.FillOp>(
            f => f.Ink.Key == TimelineInkKeys.HighlightedBar));
        Assert.That(_surface.Strokes, Has.Exactly(1).Matches<RecordingTimelineSurface.StrokeOp>(
            s => s.Ink.Key == TimelineInkKeys.HighlightOutline));
    }

    [Test]
    public void Draw_WithContent_DrawsLabelText()
    {
        _source.Label = "Yard 3";

        Draw(new BarContentContext(ShowContent: true, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Texts, Has.Exactly(1).Matches<RecordingTimelineSurface.TextOp>(
            t => t.Text == "Yard 3 (M1.1) · on 3 s" && t.Ink.Key == TimelineInkKeys.BarText));
    }

    [Test]
    public void Draw_WithoutContent_DrawsNoText()
    {
        Draw(new BarContentContext(ShowContent: false, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Texts, Is.Empty);
    }

    [Test]
    public void Draw_StoppedInterval_DrawsStoppedFlag()
    {
        _interval.End = DateTimeOffset.UnixEpoch.AddSeconds(3);
        _interval.EndReason = IntervalEndReason.Stopped;

        Draw(new BarContentContext(ShowContent: false, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Fills, Has.Some.Matches<RecordingTimelineSurface.FillOp>(
            f => f.Ink.Key == TimelineInkKeys.StoppedFlag));
    }

    [Test]
    public void Draw_FallingEdgeInterval_DrawsNoStoppedFlag()
    {
        _interval.End = DateTimeOffset.UnixEpoch.AddSeconds(3);
        _interval.EndReason = IntervalEndReason.FallingEdge;

        Draw(new BarContentContext(ShowContent: false, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Fills, Has.None.Matches<RecordingTimelineSurface.FillOp>(
            f => f.Ink.Key == TimelineInkKeys.StoppedFlag));
    }
}
