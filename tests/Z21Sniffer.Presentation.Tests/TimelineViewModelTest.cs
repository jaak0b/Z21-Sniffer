using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TimelineViewModelTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private static readonly SensorKey SensorA = new(1, 1);
    private static readonly SensorKey SensorB = new(1, 2);
    private static readonly SensorKey SensorC = new(1, 3);

    private StubClock _clock = null!;
    private TimelineViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new StubClock();
        _vm = new TimelineViewModel(new FeedbackRecorder(_clock), new SensorLabeler(), _clock, [], []);
    }

    private static IReadOnlyList<SensorState> Frame(SensorKey sensor, bool occupied) =>
        [new SensorState(sensor, occupied)];

    [Test]
    public void OnFeedback_NewSensorActivity_AddsRow()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(_vm.Rows, Has.Count.EqualTo(1));
        Assert.That(_vm.Rows[0].Sensor, Is.EqualTo(SensorA));
        Assert.That(_vm.Rows[0].Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public void OnFeedback_SameSensorTwice_DoesNotDuplicateRow()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        _clock.Now = _clock.Now.AddSeconds(1);
        _vm.OnFeedback(Frame(SensorA, occupied: false));

        Assert.That(_vm.Rows, Has.Count.EqualTo(1));
    }

    [Test]
    public void OnFeedback_RecordsIntervalForOccupiedSensor()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(_vm.Intervals, Has.Count.EqualTo(1));
        Assert.That(_vm.Intervals[0].Sensor, Is.EqualTo(SensorA));
    }

    [Test]
    public void OnFeedback_RisingEdge_RaisesSensorEdgeDetectedWithLabel()
    {
        var vm = new TimelineViewModel(new FeedbackRecorder(_clock), new SensorLabeler(), _clock,
            [new SensorAlias(SensorA, "Yard 3")], []);
        SensorEdgeLabeled? edge = null;
        vm.SensorEdgeDetected += (_, e) => edge = e;

        vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(edge, Is.Not.Null);
        Assert.That(edge!.Label, Is.EqualTo("Yard 3"));
        Assert.That(edge.Sensor, Is.EqualTo(SensorA));
        Assert.That(edge.Occupied, Is.True);
    }

    [Test]
    public void OnFeedback_RaisesChanged()
    {
        var raised = false;
        _vm.Changed += (_, _) => raised = true;

        _vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(raised, Is.True);
    }

    [Test]
    public void OnFeedback_AfterClear_ReAddsRowForSameSensor()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        _vm.ClearCommand.Execute(null);

        _clock.Now = _clock.Now.AddSeconds(1);
        _vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(_vm.Rows.Count(r => r.Sensor == SensorA), Is.EqualTo(1));
    }

    [Test]
    public void OnFeedback_InsertsRowsInSavedOrderRegardlessOfFiringOrder()
    {
        var vm = new TimelineViewModel(new FeedbackRecorder(_clock), new SensorLabeler(), _clock,
            [], savedOrder: [SensorA, SensorB, SensorC]);

        vm.OnFeedback(Frame(SensorC, occupied: true));
        vm.OnFeedback(Frame(SensorA, occupied: true));
        vm.OnFeedback(Frame(SensorB, occupied: true));

        Assert.That(vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorA, SensorB, SensorC }));
    }

    private TimelineViewModel ViewportVm()
    {
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        var vm = new TimelineViewModel(new FeedbackRecorder(new StubClock()), new SensorLabeler(), _clock, [], []);
        vm.Tick();
        return vm;
    }

    [Test]
    public void OnFeedback_NewSensor_RaisesRowsReordered()
    {
        var raised = false;
        _vm.RowsReordered += (_, _) => raised = true;

        _vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(raised, Is.True);
    }

    [Test]
    public void RemoveSensor_ThenFeedbackSameSensor_ReAddsRow()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        _vm.RemoveSensor(SensorA);
        _clock.Now = _clock.Now.AddSeconds(1);

        _vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(_vm.Rows.Count(r => r.Sensor == SensorA), Is.EqualTo(1));
    }

    [Test]
    public void RemoveSensor_DropsAlias()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        _vm.Rename(SensorA, "Yard 3");

        _vm.RemoveSensor(SensorA);

        Assert.That(_vm.Aliases.Any(a => a.Sensor == SensorA), Is.False);
    }

    [Test]
    public void RemoveSensor_UnknownSensor_DoesNotThrow() =>
        Assert.That(() => _vm.RemoveSensor(SensorA), Throws.Nothing);

    [Test]
    public void RemoveSensor_RaisesChanged()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        var raised = false;
        _vm.Changed += (_, _) => raised = true;

        _vm.RemoveSensor(SensorA);

        Assert.That(raised, Is.True);
    }

    [Test]
    public void OnFeedback_SavedSensorAfterUnsavedRow_InsertsBeforeUnsaved()
    {
        var vm = new TimelineViewModel(new FeedbackRecorder(_clock), new SensorLabeler(), _clock,
            [], savedOrder: [SensorA]);

        vm.OnFeedback(Frame(SensorC, occupied: true));
        vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorA, SensorC }));
    }

    [Test]
    public void RemoveSensor_RaisesAliasesChanged()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        var raised = false;
        _vm.AliasesChanged += (_, _) => raised = true;

        _vm.RemoveSensor(SensorA);

        Assert.That(raised, Is.True);
    }

    [Test]
    public void MoveRow_RaisesChanged()
    {
        _vm.OnFeedback([new SensorState(SensorA, true), new SensorState(SensorB, true)]);
        var raised = false;
        _vm.Changed += (_, _) => raised = true;

        _vm.MoveRow(0, 1);

        Assert.That(raised, Is.True);
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
    public void ZoomByFactor_StopsFollowingAndRaisesChanged()
    {
        var vm = ViewportVm();
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.ZoomByFactor(0.5, 0.5);

        Assert.That(vm.Following, Is.False);
        Assert.That(raised, Is.True);
    }

    [Test]
    public void SetScrollSeconds_RaisesChanged()
    {
        var vm = ViewportVm();
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.SetScrollSeconds(100);

        Assert.That(raised, Is.True);
    }

    [Test]
    public void SetScrollSeconds_JustBelowHalfSecondOfMax_StillFollows()
    {
        var vm = ViewportVm();

        vm.SetScrollSeconds(vm.ScrollMaxSeconds - 0.5);

        Assert.That(vm.Following, Is.True);
    }

    [Test]
    public void JumpToLive_RaisesChangedAndUpdatesViewportEndToNow()
    {
        var vm = ViewportVm();
        vm.PanBySeconds(-100);
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.JumpToLiveCommand.Execute(null);

        Assert.That(raised, Is.True);
        Assert.That(vm.ViewportEnd, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void TogglePause_Resume_RaisesChangedAndUpdatesViewportEnd()
    {
        var vm = ViewportVm();
        vm.TogglePauseCommand.Execute(null);
        _clock.Now = _clock.Now.AddSeconds(30);
        var raised = false;
        vm.Changed += (_, _) => raised = true;

        vm.TogglePauseCommand.Execute(null);

        Assert.That(raised, Is.True);
        Assert.That(vm.Following, Is.True);
        Assert.That(vm.ViewportEnd, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void ClearCommand_RaisesRowsReorderedAndChanged()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        var reordered = false;
        var changed = false;
        _vm.RowsReordered += (_, _) => reordered = true;
        _vm.Changed += (_, _) => changed = true;

        _vm.ClearCommand.Execute(null);

        Assert.That(reordered, Is.True);
        Assert.That(changed, Is.True);
    }

    [Test]
    public void OnFeedback_SavedOrder_FeedFirstThenSecond_KeepsSavedOrder()
    {
        var vm = new TimelineViewModel(new FeedbackRecorder(_clock), new SensorLabeler(), _clock,
            [], savedOrder: [SensorA, SensorB]);

        vm.OnFeedback(Frame(SensorA, occupied: true));
        vm.OnFeedback(Frame(SensorB, occupied: true));

        Assert.That(vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorA, SensorB }));
    }

    [Test]
    public void OnFeedback_UnsavedSensor_AppendsAfterSavedOnes()
    {
        var vm = new TimelineViewModel(new FeedbackRecorder(_clock), new SensorLabeler(), _clock,
            [], savedOrder: [SensorA, SensorB]);

        vm.OnFeedback(Frame(SensorB, occupied: true));
        vm.OnFeedback(Frame(SensorC, occupied: true));
        vm.OnFeedback(Frame(SensorA, occupied: true));

        Assert.That(vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorA, SensorB, SensorC }));
    }

    [Test]
    public void ClearCommand_RemovesRowsAndIntervals()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));

        _vm.ClearCommand.Execute(null);

        Assert.That(_vm.Rows, Is.Empty);
        Assert.That(_vm.Intervals, Is.Empty);
    }

    [Test]
    public void RemoveSensor_RemovesRowIntervalsAndRaisesRowsReordered()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        _vm.OnFeedback(Frame(SensorB, occupied: true));
        var reordered = false;
        _vm.RowsReordered += (_, _) => reordered = true;

        _vm.RemoveSensor(SensorA);

        Assert.That(_vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorB }));
        Assert.That(_vm.Intervals.Any(i => i.Sensor == SensorA), Is.False);
        Assert.That(reordered, Is.True);
    }

    [Test]
    public void MoveRow_ReordersRowsAndRaisesRowsReordered()
    {
        _vm.OnFeedback([new SensorState(SensorA, true), new SensorState(SensorB, true)]);
        var reordered = false;
        _vm.RowsReordered += (_, _) => reordered = true;

        _vm.MoveRow(0, 1);

        Assert.That(_vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorB, SensorA }));
        Assert.That(reordered, Is.True);
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
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        var vm = new TimelineViewModel(new FeedbackRecorder(new StubClock()), new SensorLabeler(), _clock, [], []);
        vm.Tick();
        var before = vm.ViewportEnd;

        vm.PanBySeconds(-100);

        Assert.That(vm.Following, Is.False);
        Assert.That(vm.ViewportEnd, Is.EqualTo(before.AddSeconds(-100)));
    }

    [Test]
    public void SetScrollSeconds_BelowMax_StopsFollowing()
    {
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        var vm = new TimelineViewModel(new FeedbackRecorder(new StubClock()), new SensorLabeler(), _clock, [], []);
        vm.Tick();

        vm.SetScrollSeconds(100);

        Assert.That(vm.Following, Is.False);
        Assert.That(vm.ViewportStart, Is.EqualTo(DateTimeOffset.UnixEpoch.AddSeconds(100)));
    }

    [Test]
    public void SetScrollSeconds_AtRightEdge_ResumesFollowing()
    {
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        var vm = new TimelineViewModel(new FeedbackRecorder(new StubClock()), new SensorLabeler(), _clock, [], []);
        vm.Tick();
        vm.SetScrollSeconds(100);

        vm.SetScrollSeconds(vm.ScrollMaxSeconds);

        Assert.That(vm.Following, Is.True);
    }

    [Test]
    public void ZoomByFactor_ZoomingIn_ReducesWindowSecondsAndGrowsScrollMax()
    {
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(600);
        var vm = new TimelineViewModel(new FeedbackRecorder(new StubClock()), new SensorLabeler(), _clock, [], []);
        vm.Tick();
        var windowBefore = vm.WindowSeconds;
        var maxBefore = vm.ScrollMaxSeconds;

        vm.ZoomByFactor(0.5, 0.5);

        Assert.That(vm.WindowSeconds, Is.LessThan(windowBefore));
        Assert.That(vm.ScrollMaxSeconds, Is.GreaterThan(maxBefore));
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

    [Test]
    public void CommitRenameCommand_PersistsEditedRowLabelAsAlias()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        var row = _vm.Rows[0];
        row.Label = "Yard 3";

        _vm.CommitRenameCommand.Execute(row);

        Assert.That(_vm.Aliases.Single(a => a.Sensor == SensorA).Name, Is.EqualTo("Yard 3"));
    }

    [Test]
    public void Rename_CalledTwiceForSameSensor_KeepsSingleAlias()
    {
        _vm.Rename(SensorA, "first");
        _vm.Rename(SensorA, "second");

        Assert.That(_vm.Aliases.Count(a => a.Sensor == SensorA), Is.EqualTo(1));
        Assert.That(_vm.Aliases.Single(a => a.Sensor == SensorA).Name, Is.EqualTo("second"));
    }

    [Test]
    public void Rename_UpdatesRowLabelAndRaisesAliasesChanged()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        var raised = false;
        _vm.AliasesChanged += (_, _) => raised = true;

        _vm.Rename(SensorA, "Station track 2");

        Assert.That(_vm.Rows[0].Label, Is.EqualTo("Station track 2"));
        Assert.That(raised, Is.True);
    }

    [Test]
    public void LoadSession_RestoresIntervalsAndRowPerDistinctSensor()
    {
        var startedAt = _clock.Now - TimeSpan.FromMinutes(3);
        var session = new RecordingSession(startedAt,
        [
            new SensorInterval(SensorA, startedAt, startedAt + TimeSpan.FromSeconds(1)),
            new SensorInterval(SensorB, startedAt + TimeSpan.FromSeconds(2), End: null),
            new SensorInterval(SensorA, startedAt + TimeSpan.FromSeconds(3), startedAt + TimeSpan.FromSeconds(4)),
        ]);

        _vm.LoadSession(session);

        Assert.That(_vm.Intervals, Has.Count.EqualTo(3));
        Assert.That(_vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorA, SensorB }));
    }

    [Test]
    public void LoadSession_ClearsPreviousRowsAndIntervals()
    {
        _vm.OnFeedback(Frame(SensorC, occupied: true));
        var startedAt = _clock.Now - TimeSpan.FromMinutes(1);
        var session = new RecordingSession(startedAt,
        [
            new SensorInterval(SensorB, startedAt, startedAt + TimeSpan.FromSeconds(1)),
        ]);

        _vm.LoadSession(session);

        Assert.That(_vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorB }));
        Assert.That(_vm.Intervals.Select(i => i.Sensor), Is.EqualTo(new[] { SensorB }));
    }

    [Test]
    public void LoadSession_RaisesChanged()
    {
        var raised = false;
        _vm.Changed += (_, _) => raised = true;
        var startedAt = _clock.Now - TimeSpan.FromMinutes(1);
        var session = new RecordingSession(startedAt,
        [
            new SensorInterval(SensorA, startedAt, startedAt + TimeSpan.FromSeconds(1)),
        ]);

        _vm.LoadSession(session);

        Assert.That(raised, Is.True);
    }

    [Test]
    public void LoadSession_RaisesRowsReordered()
    {
        var raised = false;
        _vm.RowsReordered += (_, _) => raised = true;
        var startedAt = _clock.Now - TimeSpan.FromMinutes(1);
        var session = new RecordingSession(startedAt,
        [
            new SensorInterval(SensorA, startedAt, startedAt + TimeSpan.FromSeconds(1)),
        ]);

        _vm.LoadSession(session);

        Assert.That(raised, Is.True);
    }

    [Test]
    public void LoadSession_WhenSensorAlreadyKnown_RebuildsItsRowExactlyOnce()
    {
        _vm.OnFeedback(Frame(SensorA, occupied: true));
        var startedAt = _clock.Now - TimeSpan.FromMinutes(1);
        var session = new RecordingSession(startedAt,
        [
            new SensorInterval(SensorA, startedAt, startedAt + TimeSpan.FromSeconds(1)),
        ]);

        _vm.LoadSession(session);

        Assert.That(_vm.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { SensorA }));
    }
}
