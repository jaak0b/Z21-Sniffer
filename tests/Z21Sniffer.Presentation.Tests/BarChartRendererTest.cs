using Autofac.Features.Indexed;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class BarChartRendererTest
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
    private static readonly SensorKey SensorA = new(1, 1);
    private static readonly SensorKey SensorB = new(1, 2);

    private RecordingTimelineSurface _surface = null!;
    private BarChartRenderer _renderer = null!;

    [SetUp]
    public void SetUp()
    {
        _surface = new RecordingTimelineSurface();
        _renderer = new BarChartRenderer(Index(
            (typeof(FeedbackSensorInterval), new SensorIntervalChartDrawingStrategy()),
            (typeof(ConnectionInterval), new ConnectionIntervalChartDrawingStrategy()),
            (typeof(LocoInterval), new LocoIntervalChartDrawingStrategy())));
    }

    private static IIndex<Type, IIntervalChartDrawingStrategy> Index(
        params (Type Key, IIntervalChartDrawingStrategy Strategy)[] pairs) =>
        new FakeIndex<Type, IIntervalChartDrawingStrategy>(pairs.ToDictionary(p => p.Key, p => p.Strategy));

    private static ChartViewport Viewport() => new(T0, T0.AddSeconds(10), Width: 1000);

    private void Render(IReadOnlyList<IIntervalSource> sources, double verticalOffset = 0, double visibleHeight = 1000) =>
        _renderer.Render(_surface, sources, Viewport(), T0.AddSeconds(10),
            highlightUnderSeconds: null, verticalOffset, visibleHeight, minContentWidth: 50, zoomFraction: 1);

    private static FeedbackSensorSource Sensor(SensorKey key, int order, params (double Start, double? End)[] intervals)
    {
        var source = new FeedbackSensorSource { Id = $"sensor:{key.Module}.{key.Contact}", Sensor = key, Label = $"M{key.Module}.{key.Contact}" };
        foreach (var (start, end) in intervals)
        {
            source.Apply(occupied: true, T0.AddSeconds(start));
            if (end is { } e) source.Apply(occupied: false, T0.AddSeconds(e));
        }

        return source;
    }

    [Test]
    public void Render_DrawsEachSourceWithItsKeyedStrategy()
    {
        var sensor = Sensor(SensorA, order: 0, (2, null));
        var connection = new ConnectionSource { Id = "connection" };
        connection.Set(connected: true, T0.AddSeconds(2));

        Render(new IIntervalSource[] { sensor, connection });

        Assert.That(_surface.Fills, Has.Some.Matches<RecordingTimelineSurface.FillOp>(f => f.Ink.Key == TimelineInkKeys.Bar));
        Assert.That(_surface.Fills, Has.Some.Matches<RecordingTimelineSurface.FillOp>(f => f.Ink.Key == TimelineInkKeys.Connected));
    }

    [Test]
    public void Render_StacksLanesByOrder()
    {
        var first = Sensor(SensorA, order: 0, (2, 4));
        var second = Sensor(SensorB, order: 1, (2, 4));

        Render(new IIntervalSource[] { first, second });

        var firstRect = _surface.Fills[0].Rect;
        var secondRect = _surface.Fills[1].Rect;
        Assert.That(firstRect.Y, Is.EqualTo(0));
        Assert.That(secondRect.Y, Is.GreaterThan(firstRect.Y));
    }

    [Test]
    public void Render_IteratesEveryIntervalOfASource()
    {
        var sensor = Sensor(SensorA, order: 0, (1, 2), (3, 4), (5, 6));

        Render(new IIntervalSource[] { sensor });

        Assert.That(_surface.Fills.Count(f => f.Ink.Key == TimelineInkKeys.Bar), Is.EqualTo(3));
    }

    [Test]
    public void Render_TouchingIntervals_SquareOnlyTheirSharedCorners()
    {
        var connection = new ConnectionSource { Id = "connection" };
        connection.Set(connected: true, T0.AddSeconds(1));
        connection.Set(connected: false, T0.AddSeconds(4));

        Render(new IIntervalSource[] { connection });

        var bars = _surface.Fills
            .Where(f => f.Ink.Key is TimelineInkKeys.Connected or TimelineInkKeys.Disconnected)
            .ToList();
        Assert.That(bars, Has.Count.EqualTo(2));
        Assert.That(bars[0].Rect.Corners, Is.EqualTo(new BarCorners(SquareLeft: false, SquareRight: true)));
        Assert.That(bars[1].Rect.Corners, Is.EqualTo(new BarCorners(SquareLeft: true, SquareRight: false)));
    }

    [Test]
    public void Render_SeparatedBars_KeepBothEndsRounded()
    {
        var sensor = Sensor(SensorA, order: 0, (1, 2), (4, 5));

        Render(new IIntervalSource[] { sensor });

        var bars = _surface.Fills.Where(f => f.Ink.Key == TimelineInkKeys.Bar).ToList();
        Assert.That(bars, Has.Count.EqualTo(2));
        Assert.That(bars, Has.All.Matches<RecordingTimelineSurface.FillOp>(f =>
            f.Rect.Corners == new BarCorners(SquareLeft: false, SquareRight: false)));
    }

    [Test]
    public void Render_NarrowBar_DrawsNoContent()
    {
        var sensor = Sensor(SensorA, order: 0, (2, 2.02));

        Render(new IIntervalSource[] { sensor });

        Assert.That(_surface.Texts, Is.Empty);
    }

    [Test]
    public void Render_WideBar_DrawsContent()
    {
        var sensor = Sensor(SensorA, order: 0, (2, 6));

        Render(new IIntervalSource[] { sensor });

        Assert.That(_surface.Texts, Has.Count.EqualTo(1));
    }

    private void RenderWithThreshold(IReadOnlyList<IIntervalSource> sources, double threshold) =>
        _renderer.Render(_surface, sources, Viewport(), T0.AddSeconds(10),
            highlightUnderSeconds: threshold, verticalOffset: 0, visibleHeight: 1000, minContentWidth: 50, zoomFraction: 1);

    [Test]
    public void Render_ShortInterval_OutlinesItWhenUnderThreshold()
    {
        var sensor = Sensor(SensorA, order: 0, (2, 6));

        RenderWithThreshold(new IIntervalSource[] { sensor }, threshold: 10);

        Assert.That(_surface.Strokes, Has.Some.Matches<RecordingTimelineSurface.StrokeOp>(s => s.Ink.Key == TimelineInkKeys.HighlightOutline));
    }

    [Test]
    public void Render_ShortButStillOpenInterval_DrawsNoHighlightOutline()
    {
        var sensor = Sensor(SensorA, order: 0, (9.5, null));

        RenderWithThreshold(new IIntervalSource[] { sensor }, threshold: 10);

        Assert.That(_surface.Strokes, Has.None.Matches<RecordingTimelineSurface.StrokeOp>(s => s.Ink.Key == TimelineInkKeys.HighlightOutline));
    }

    [Test]
    public void Render_IntervalInsideTheBand_IsOutlined()
    {
        var sensor = Sensor(SensorA, order: 0, (2, 2.3));

        _renderer.Render(_surface, new IIntervalSource[] { sensor }, Viewport(), T0.AddSeconds(10),
            highlightUnderSeconds: 0.5, verticalOffset: 0, visibleHeight: 1000, minContentWidth: 50, zoomFraction: 1,
            highlightOverSeconds: 0.1);

        Assert.That(_surface.Strokes, Has.Some.Matches<RecordingTimelineSurface.StrokeOp>(s => s.Ink.Key == TimelineInkKeys.HighlightOutline));
    }

    [Test]
    public void Render_IntervalShorterThanTheLowerBound_IsNotOutlined()
    {
        var sensor = Sensor(SensorA, order: 0, (2, 2.05));

        _renderer.Render(_surface, new IIntervalSource[] { sensor }, Viewport(), T0.AddSeconds(10),
            highlightUnderSeconds: 0.5, verticalOffset: 0, visibleHeight: 1000, minContentWidth: 50, zoomFraction: 1,
            highlightOverSeconds: 0.1);

        Assert.That(_surface.Strokes, Has.None.Matches<RecordingTimelineSurface.StrokeOp>(s => s.Ink.Key == TimelineInkKeys.HighlightOutline));
    }

    [Test]
    public void Render_LongInterval_DrawsNoHighlightOutline()
    {
        var sensor = Sensor(SensorA, order: 0, (2, 6));

        RenderWithThreshold(new IIntervalSource[] { sensor }, threshold: 1);

        Assert.That(_surface.Strokes, Has.None.Matches<RecordingTimelineSurface.StrokeOp>(s => s.Ink.Key == TimelineInkKeys.HighlightOutline));
    }

    [Test]
    public void Render_ShortInterval_OnAnOptedOutSource_DrawsNoHighlightOutline()
    {
        var loco = new LocoIntervalSource { Id = "loco:3", Address = 3 };
        loco.Apply(speed: 5, forward: true, maxSpeed: 100, T0.AddSeconds(2));
        loco.Apply(speed: 0, forward: true, maxSpeed: 100, T0.AddSeconds(2.4));

        RenderWithThreshold(new IIntervalSource[] { loco }, threshold: 10);

        Assert.That(_surface.Strokes, Has.None.Matches<RecordingTimelineSurface.StrokeOp>(s => s.Ink.Key == TimelineInkKeys.HighlightOutline));
    }

    [Test]
    public void Render_SkipsLanesOutsideVisibleBand()
    {
        var first = Sensor(SensorA, order: 0, (2, 4));
        var second = Sensor(SensorB, order: 1, (2, 4));

        Render(new IIntervalSource[] { first, second }, verticalOffset: 0, visibleHeight: 20);

        Assert.That(_surface.Fills, Has.Count.EqualTo(1));
    }

    [Test]
    public void Render_MissingStrategyForType_Throws()
    {
        var renderer = new BarChartRenderer(Index());
        var sensor = Sensor(SensorA, order: 0, (2, 4));

        Assert.That(() => renderer.Render(_surface, new IIntervalSource[] { sensor }, Viewport(), T0.AddSeconds(10),
            null, 0, 1000, 50, 1), Throws.Exception);
    }
}
