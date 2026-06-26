using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class RecordingViewModelTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    [TearDown]
    public void ResetToEnglish() => LocalizationService.Instance.Apply("en");

    private StubClock _inner = null!;
    private RecordingClock _clock = null!;
    private IntervalSourceRegistry _registry = null!;
    private bool _connected;
    private RecordingViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _inner = new StubClock();
        _clock = new RecordingClock(_inner);
        _registry = new IntervalSourceRegistry();
        _connected = true;
        _vm = new RecordingViewModel(_registry, _clock, () => _connected);
    }

    private ConnectionSource Connection() => (ConnectionSource)_registry.Find("connection")!;

    [Test]
    public void Start_StartsClockOpensConnectionSourceAndSetsRecording()
    {
        _vm.ToggleCommand.Execute(null);

        Assert.That(_clock.IsRunning, Is.True);
        Assert.That(_vm.IsRecording, Is.True);
        Assert.That(_registry.Find("connection"), Is.Not.Null);
    }

    [Test]
    public void Start_RecordsCurrentConnectionState()
    {
        _connected = true;

        _vm.ToggleCommand.Execute(null);

        Assert.That(Connection().Intervals.Single().Connected, Is.True);
    }

    [Test]
    public void Start_ClearsPriorData()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1", s => s.Sensor = new SensorKey(1, 1))
            .Apply(occupied: true, _inner.Now);

        _vm.ToggleCommand.Execute(null);

        Assert.That(_registry.Find("sensor:1.1"), Is.Null);
    }

    [Test]
    public void Stop_StopsClockClosesOpenIntervalsAsStoppedAndClearsRecording()
    {
        _vm.ToggleCommand.Execute(null);
        _inner.Now = _inner.Now.AddSeconds(5);

        _vm.ToggleCommand.Execute(null);

        Assert.That(_clock.IsRunning, Is.False);
        Assert.That(_vm.IsRecording, Is.False);
        var interval = Connection().Intervals.Single();
        Assert.That(interval.IsOpen, Is.False);
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.Stopped));
    }

    [Test]
    public void Stop_ClosesEveryOpenSourceNotJustConnection()
    {
        _vm.ToggleCommand.Execute(null);
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1", s => s.Sensor = new SensorKey(1, 1))
            .Apply(occupied: true, _clock.Now);
        _inner.Now = _inner.Now.AddSeconds(5);

        _vm.ToggleCommand.Execute(null);

        var sensor = (FeedbackSensorSource)_registry.Find("sensor:1.1")!;
        Assert.That(sensor.Intervals.Single().EndReason, Is.EqualTo(IntervalEndReason.Stopped));
    }

    [Test]
    public void ToggleCommand_IsExecutableEvenWhenDisconnected()
    {
        _connected = false;

        Assert.That(_vm.ToggleCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void Toggle_WhenDisconnected_StartsAndRecordsDisconnectedState()
    {
        _connected = false;

        _vm.ToggleCommand.Execute(null);

        Assert.That(_vm.IsRecording, Is.True);
        Assert.That(_clock.IsRunning, Is.True);
        Assert.That(Connection().Intervals.Single().Connected, Is.False);
    }

    [Test]
    public void ShouldRecordFeedback_EqualsIsRecording()
    {
        Assert.That(_vm.ShouldRecordFeedback, Is.False);

        _vm.ToggleCommand.Execute(null);

        Assert.That(_vm.ShouldRecordFeedback, Is.True);
    }

    [Test]
    public void ToggleLabel_IsStartBeforeRecordingAndStopWhileRecording()
    {
        Assert.That(_vm.ToggleLabel, Is.EqualTo(LocalizationService.Instance["StartRecording"]));

        _vm.ToggleCommand.Execute(null);

        Assert.That(_vm.ToggleLabel, Is.EqualTo(LocalizationService.Instance["StopRecording"]));
    }

    [Test]
    public void IsRecording_Change_RaisesToggleLabelChanged()
    {
        var changed = false;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RecordingViewModel.ToggleLabel)) changed = true;
        };

        _vm.ToggleCommand.Execute(null);

        Assert.That(changed, Is.True);
    }

    [Test]
    public void LanguageChange_RaisesToggleLabelChanged()
    {
        var changed = false;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RecordingViewModel.ToggleLabel)) changed = true;
        };

        LocalizationService.Instance.Apply("de");

        Assert.That(changed, Is.True);
    }

    [Test]
    public void FreshStart_ResetsClockAfterStop()
    {
        _vm.ToggleCommand.Execute(null);
        _inner.Now = _inner.Now.AddSeconds(5);
        _vm.ToggleCommand.Execute(null);
        _inner.Now = _inner.Now.AddSeconds(9);

        _vm.ToggleCommand.Execute(null);

        Assert.That(_clock.Now, Is.EqualTo(_inner.Now));
    }
}
