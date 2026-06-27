using Autofac.Features.Indexed;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TimelineViewModelTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private sealed class StubRemovalConfirmation : IRemovalConfirmation
    {
        public Task<bool> ConfirmAsync() => Task.FromResult(true);
    }

    private static readonly SensorKey SensorA = new(1, 1);
    private static readonly SensorKey SensorB = new(1, 2);

    private StubClock _clock = null!;
    private IntervalSourceRegistry _registry = null!;
    private FeedbackSensorIngest _ingest = null!;
    private TimelineViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new StubClock();
        _vm = Build();
    }

    private TimelineViewModel Build()
    {
        _registry = new IntervalSourceRegistry();
        _ingest = new FeedbackSensorIngest(_registry);
        var chart = new FakeIndex<Type, IIntervalChartDrawingStrategy>(new Dictionary<Type, IIntervalChartDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalChartDrawingStrategy(),
            [typeof(ConnectionInterval)] = new ConnectionIntervalChartDrawingStrategy(),
            [typeof(LocoInterval)] = new LocoIntervalChartDrawingStrategy(),
        });
        var legend = new FakeIndex<Type, IIntervalLegendDrawingStrategy>(new Dictionary<Type, IIntervalLegendDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation()),
            [typeof(ConnectionInterval)] = new ConnectionIntervalLegendDrawingStrategy(),
            [typeof(LocoInterval)] = new LocoIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation()),
        });
        return new TimelineViewModel(_registry, chart, legend, _clock);
    }

    private TimelineViewModel ViewportVm()
    {
        var vm = Build();
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        vm.Tick();
        return vm;
    }

    private void Feed(SensorKey sensor, bool occupied) =>
        _ingest.Apply([new SensorState(sensor, occupied)], _clock.Now);

    private static IReadOnlyList<SensorKey> Lane(TimelineViewModel vm) =>
        vm.LegendRows.Select(row => row.Source).OfType<FeedbackSensorSource>().Select(s => s.Sensor).ToList();

    [Test]
    public void NewSensorActivity_AddsLegendRow()
    {
        Feed(SensorA, occupied: true);

        Assert.That(_vm.LegendRows, Has.Count.EqualTo(1));
        Assert.That(Lane(_vm), Is.EqualTo(new[] { SensorA }));
    }

    [Test]
    public void SameSensorTwice_DoesNotDuplicateRow()
    {
        Feed(SensorA, occupied: true);
        _clock.Now = _clock.Now.AddSeconds(1);
        Feed(SensorA, occupied: false);

        Assert.That(_vm.LegendRows, Has.Count.EqualTo(1));
    }

    [Test]
    public void NewSensor_RaisesRowsReorderedAndChanged()
    {
        var reordered = false;
        var changed = false;
        _vm.RowsReordered += (_, _) => reordered = true;
        _vm.Changed += (_, _) => changed = true;

        Feed(SensorA, occupied: true);

        Assert.That(reordered, Is.True);
        Assert.That(changed, Is.True);
    }

    [Test]
    public void RemovingSourceFromRegistry_DropsRow()
    {
        Feed(SensorA, occupied: true);
        Feed(SensorB, occupied: true);

        _registry.Remove(_registry.Sources.First());

        Assert.That(_vm.LegendRows, Has.Count.EqualTo(1));
    }

    [Test]
    public void MoveRow_ReordersRowsSetsOrderAndRaises()
    {
        Feed(SensorA, occupied: true);
        Feed(SensorB, occupied: true);
        var reordered = false;
        _vm.RowsReordered += (_, _) => reordered = true;

        _vm.MoveRow(0, 1);

        Assert.That(Lane(_vm), Is.EqualTo(new[] { SensorB, SensorA }));
        Assert.That(_vm.Sources.Select(s => s.Order), Is.Ordered);
        Assert.That(reordered, Is.True);
    }

    [Test]
    public void LocoRow_IsTallerThanSensorRow()
    {
        Feed(SensorA, occupied: true);
        _registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3).Apply(40, forward: true, maxSpeed: 126, _clock.Now);
        _vm.Tick();

        var sensorRow = _vm.LegendRows.Single(row => row.Source is FeedbackSensorSource);
        var locoRow = _vm.LegendRows.Single(row => row.Source is LocoIntervalSource);
        Assert.That(locoRow.Height, Is.GreaterThan(sensorRow.Height));
    }

    [Test]
    public void ZoomingOut_ShrinksLocoRowTowardBaseHeight()
    {
        var vm = ViewportVm();
        _registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3).Apply(40, forward: true, maxSpeed: 126, _clock.Now);
        vm.Tick();
        var tall = vm.LegendRows.Single().Height;

        vm.ZoomByFactor(50, 0.5);

        Assert.That(vm.LegendRows.Single().Height, Is.LessThan(tall));
    }

    [Test]
    public void LocoIntervalWithSamples_RendersAsAMultiPointSpeedLine()
    {
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        var loco = _registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3);
        loco.Apply(40, forward: true, maxSpeed: 126, _clock.Now.AddSeconds(-5));
        loco.Apply(80, forward: true, maxSpeed: 126, _clock.Now.AddSeconds(-3));
        loco.Apply(120, forward: true, maxSpeed: 126, _clock.Now.AddSeconds(-1));
        _vm.Tick();

        var surface = new RecordingTimelineSurface();
        _vm.Renderer.Render(surface, _vm.Sources,
            new ChartViewport(_vm.ViewportStart, _vm.ViewportEnd, 1000), _vm.ViewportEnd,
            _vm.HighlightUnderSeconds, verticalOffset: 0, visibleHeight: 1000, minContentWidth: 52, _vm.ZoomFraction);

        Assert.That(surface.Polylines, Has.Some.Matches<RecordingTimelineSurface.PolylineOp>(p => p.Points.Count >= 2));
    }

    [Test]
    public void NewSourceRow_GetsZoomScaledHeightImmediatelyOnReconcile()
    {
        _registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3).Apply(40, forward: true, maxSpeed: 126, _clock.Now);

        Assert.That(_vm.LegendRows.Single().Height, Is.GreaterThan(26));
    }

    [Test]
    public void DefaultWindow_ScalesLocoRowByLogZoomFraction()
    {
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        _registry.GetOrCreate<LocoIntervalSource>("loco:3", source => source.Address = 3).Apply(40, forward: true, maxSpeed: 126, _clock.Now);
        _vm.Tick();

        Assert.That(_vm.LegendRows.Single().Height, Is.EqualTo(42.63).Within(0.05));
    }

    [Test]
    public void ReconcileWithUnchangedSources_DoesNotRebuildRowsAgain()
    {
        Feed(SensorA, occupied: true);
        Feed(SensorB, occupied: true);
        var reorderedCount = 0;
        _vm.RowsReordered += (_, _) => reorderedCount++;

        _clock.Now = _clock.Now.AddSeconds(1);
        Feed(SensorB, occupied: false);

        Assert.That(reorderedCount, Is.EqualTo(0));
    }

    [Test]
    public void ReconcileAfterMoveRow_DoesNotRebuildRows()
    {
        Feed(SensorA, occupied: true);
        Feed(SensorB, occupied: true);
        _vm.MoveRow(0, 1);
        var reorderedCount = 0;
        _vm.RowsReordered += (_, _) => reorderedCount++;

        _clock.Now = _clock.Now.AddSeconds(1);
        Feed(SensorB, occupied: false);

        Assert.That(reorderedCount, Is.EqualTo(0));
    }

    [Test]
    public void ClearCommand_WithNoRows_StillRaisesChanged()
    {
        var changed = false;
        _vm.Changed += (_, _) => changed = true;

        _vm.ClearCommand.Execute(null);

        Assert.That(changed, Is.True);
    }

    [Test]
    public void LoadSession_WithSameSourceIds_StillRaisesChanged()
    {
        Feed(SensorA, occupied: true);
        var reloaded = new FeedbackSensorSource { Id = "sensor:1.1", Sensor = SensorA };
        reloaded.Apply(occupied: true, _clock.Now);
        var changed = false;
        _vm.Changed += (_, _) => changed = true;

        _vm.LoadSession(new RecordingSession(_clock.Now, new IIntervalSource[] { reloaded }));

        Assert.That(changed, Is.True);
    }

    [Test]
    public void MoveRow_RaisesChanged()
    {
        Feed(SensorA, occupied: true);
        Feed(SensorB, occupied: true);
        var changed = false;
        _vm.Changed += (_, _) => changed = true;

        _vm.MoveRow(0, 1);

        Assert.That(changed, Is.True);
    }

    [Test]
    public void ZoomByFactor_StopsFollowingAndRaisesChanged()
    {
        var vm = ViewportVm();
        var changed = false;
        vm.Changed += (_, _) => changed = true;

        vm.ZoomByFactor(0.5, 0.5);

        Assert.That(vm.Following, Is.False);
        Assert.That(changed, Is.True);
    }

    [Test]
    public void SetScrollSeconds_RaisesChanged()
    {
        var vm = ViewportVm();
        var changed = false;
        vm.Changed += (_, _) => changed = true;

        vm.SetScrollSeconds(100);

        Assert.That(changed, Is.True);
    }

    [Test]
    public void JumpToLiveCommand_RaisesChanged()
    {
        var changed = false;
        _vm.Changed += (_, _) => changed = true;

        _vm.JumpToLiveCommand.Execute(null);

        Assert.That(changed, Is.True);
    }

    [Test]
    public void TogglePauseCommand_RaisesChanged()
    {
        var changed = false;
        _vm.Changed += (_, _) => changed = true;

        _vm.TogglePauseCommand.Execute(null);

        Assert.That(changed, Is.True);
    }

    [Test]
    public void JumpToLive_AfterPanningBack_RefreshesViewportEndToNow()
    {
        var vm = ViewportVm();
        vm.PanBySeconds(-100);

        vm.JumpToLiveCommand.Execute(null);

        Assert.That(vm.ViewportEnd, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void TogglePause_ResumingFollow_RefreshesViewportEndToNow()
    {
        var vm = ViewportVm();
        vm.PanBySeconds(-100);

        vm.TogglePauseCommand.Execute(null);

        Assert.That(vm.Following, Is.True);
        Assert.That(vm.ViewportEnd, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void ClearCommand_RemovesRowsAndIntervals()
    {
        Feed(SensorA, occupied: true);

        _vm.ClearCommand.Execute(null);

        Assert.That(_vm.LegendRows, Is.Empty);
        Assert.That(_vm.Sources, Is.Empty);
    }

    [Test]
    public void ClearCommand_RaisesChanged()
    {
        Feed(SensorA, occupied: true);
        var changed = false;
        _vm.Changed += (_, _) => changed = true;

        _vm.ClearCommand.Execute(null);

        Assert.That(changed, Is.True);
    }

    [Test]
    public void ToSession_CapturesRegistrySourcesAndStartedAt()
    {
        Feed(SensorA, occupied: true);

        var session = _vm.ToSession();

        Assert.That(session.StartedAt, Is.EqualTo(_vm.StartedAt));
        Assert.That(session.Sources, Has.Count.EqualTo(1));
    }

    [Test]
    public void LoadSession_PopulatesRowsAndRaises()
    {
        var start = _clock.Now - TimeSpan.FromMinutes(1);
        var sa = new FeedbackSensorSource { Id = "sensor:1.1", Sensor = SensorA };
        sa.Apply(occupied: true, start);
        var sb = new FeedbackSensorSource { Id = "sensor:1.2", Sensor = SensorB };
        sb.Apply(occupied: true, start);
        var reordered = false;
        var changed = false;
        _vm.RowsReordered += (_, _) => reordered = true;
        _vm.Changed += (_, _) => changed = true;

        _vm.LoadSession(new RecordingSession(start, new IIntervalSource[] { sa, sb }));

        Assert.That(_vm.LegendRows, Has.Count.EqualTo(2));
        Assert.That(_vm.StartedAt, Is.EqualTo(start));
        Assert.That(reordered, Is.True);
        Assert.That(changed, Is.True);
    }

    [Test]
    public void Sources_ReflectsRegistry()
    {
        Feed(SensorA, occupied: true);

        Assert.That(_vm.Sources, Is.EqualTo(_registry.Sources));
    }

    [Test]
    public void TogglePauseCommand_TogglesFollowing()
    {
        Assert.That(_vm.Following, Is.True);

        _vm.TogglePauseCommand.Execute(null);

        Assert.That(_vm.Following, Is.False);
    }

    [Test]
    public void JumpToLiveCommand_ResumesFollowing()
    {
        _vm.TogglePauseCommand.Execute(null);

        _vm.JumpToLiveCommand.Execute(null);

        Assert.That(_vm.Following, Is.True);
    }

    [Test]
    public void PanBySeconds_StopsFollowingAndMovesViewportEnd()
    {
        var vm = ViewportVm();
        var before = vm.ViewportEnd;

        vm.PanBySeconds(-100);

        Assert.That(vm.Following, Is.False);
        Assert.That(vm.ViewportEnd, Is.EqualTo(before.AddSeconds(-100)));
    }

    [Test]
    public void PanBySeconds_RaisesChanged()
    {
        var vm = ViewportVm();
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.PanBySeconds(-100);

        Assert.That(raised, Is.True);
    }

    [Test]
    public void ZoomByFactor_ZoomingIn_ReducesWindowSecondsAndGrowsScrollMax()
    {
        var vm = ViewportVm();
        var windowBefore = vm.WindowSeconds;
        var maxBefore = vm.ScrollMaxSeconds;

        vm.ZoomByFactor(0.5, 0.5);

        Assert.That(vm.WindowSeconds, Is.LessThan(windowBefore));
        Assert.That(vm.ScrollMaxSeconds, Is.GreaterThan(maxBefore));
    }

    [Test]
    public void SetScrollSeconds_BelowMax_StopsFollowing()
    {
        var vm = ViewportVm();

        vm.SetScrollSeconds(100);

        Assert.That(vm.Following, Is.False);
        Assert.That(vm.ViewportStart, Is.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(100)));
    }

    [Test]
    public void SetScrollSeconds_AtRightEdge_ResumesFollowing()
    {
        var vm = ViewportVm();
        vm.SetScrollSeconds(100);

        vm.SetScrollSeconds(vm.ScrollMaxSeconds);

        Assert.That(vm.Following, Is.True);
    }

    [Test]
    public void SetScrollSeconds_JustBelowHalfSecondOfMax_StillFollows()
    {
        var vm = ViewportVm();

        vm.SetScrollSeconds(vm.ScrollMaxSeconds - 0.5);

        Assert.That(vm.Following, Is.True);
    }

    [Test]
    public void Tick_WhileFollowing_AdvancesViewportEndToNow()
    {
        _vm.Tick();
        _clock.Now = _clock.Now.AddSeconds(30);

        _vm.Tick();

        Assert.That(_vm.ViewportEnd, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void HighlightUnderSeconds_PositiveThreshold_ReturnsThreshold()
    {
        _vm.HighlightThresholdSeconds = 0.2;

        Assert.That(_vm.HighlightUnderSeconds, Is.EqualTo(0.2));
    }

    [Test]
    public void HighlightUnderSeconds_ZeroThreshold_IsNull()
    {
        _vm.HighlightThresholdSeconds = 0;

        Assert.That(_vm.HighlightUnderSeconds, Is.Null);
    }

    [Test]
    public void HighlightUnderSeconds_OnByDefault()
    {
        Assert.That(_vm.HighlightUnderSeconds, Is.EqualTo(0.5));
    }
}
