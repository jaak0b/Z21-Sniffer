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
        });
        var legend = new FakeIndex<Type, IIntervalLegendDrawingStrategy>(new Dictionary<Type, IIntervalLegendDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalLegendDrawingStrategy(_registry, new StubRemovalConfirmation()),
            [typeof(ConnectionInterval)] = new ConnectionIntervalLegendDrawingStrategy(),
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
