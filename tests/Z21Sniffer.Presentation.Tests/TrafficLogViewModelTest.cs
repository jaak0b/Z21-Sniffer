using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Logging;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TrafficLogViewModelTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private StubClock _clock = null!;
    private TrafficLogViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _clock = new StubClock();
        _vm = new TrafficLogViewModel(LocalizationService.Instance, _clock);
        _vm.StartRecording();
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void AppendSystem_AddsExactTelemetryEntry()
    {
        _vm.AppendSystem(new SystemSnapshot(320, 15000, 32, false, false, false, false, false, false));

        Assert.That(_vm.Filtered, Has.Count.EqualTo(1));
        Assert.That(_vm.Filtered[0].Kind, Is.EqualTo(LogEntryKind.System));
        Assert.That(_vm.Filtered[0].Message, Is.EqualTo("320 mA · 15.0 V · 32 °C"));
        Assert.That(_vm.Filtered[0].Fault, Is.False);
    }

    [Test]
    public void AppendSystem_ShortCircuit_AppendsFaultAfterSeparator()
    {
        _vm.AppendSystem(new SystemSnapshot(0, 0, 0, ShortCircuit: true, false, false, false, false, false));

        Assert.That(_vm.Filtered[0].Fault, Is.True);
        Assert.That(_vm.Filtered[0].Message, Is.EqualTo("0 mA · 0.0 V · 0 °C · Short circuit"));
    }

    [Test]
    public void AppendSystem_MultipleFaults_JoinedBySeparator()
    {
        _vm.AppendSystem(new SystemSnapshot(0, 0, 0, ShortCircuit: true, EmergencyStop: true, false, false, false, false));

        Assert.That(_vm.Filtered[0].Message, Does.Contain("Short circuit · Emergency stop"));
    }

    [Test]
    public void AppendSystem_RemainingFaults_AllNamed()
    {
        _vm.AppendSystem(new SystemSnapshot(0, 0, 0, false, false,
            TrackVoltageOff: true, ProgrammingMode: false, PowerLost: true, HighTemperature: true));

        var message = _vm.Filtered[0].Message;
        Assert.That(message, Does.Contain("Track voltage off"));
        Assert.That(message, Does.Contain("Power lost"));
        Assert.That(message, Does.Contain("High temperature"));
    }

    [Test]
    public void AppendSensorEdge_Occupied_FormatsLabelAddressAndVerb()
    {
        _vm.AppendSensorEdge("Yard 3", new SensorKey(1, 1), occupied: true, _clock.Now);

        var message = _vm.Filtered[0].Message;
        Assert.That(message, Does.Contain("Yard 3"));
        Assert.That(message, Does.Contain("(M1.1)"));
        Assert.That(message, Does.Contain("occupied"));
        Assert.That(_vm.Filtered[0].Kind, Is.EqualTo(LogEntryKind.Sensor));
    }

    [Test]
    public void AppendSensorEdge_NoAlias_OmitsRedundantParenthesis()
    {
        _vm.AppendSensorEdge("M1.1", new SensorKey(1, 1), occupied: false, _clock.Now);

        Assert.That(_vm.Filtered[0].Message, Does.Not.Contain("(M1.1)"));
        Assert.That(_vm.Filtered[0].Message, Does.Contain("M1.1"));
    }

    [Test]
    public void AppendSensorEdge_Clear_UsesClearVerb()
    {
        _vm.AppendSensorEdge("Yard 3", new SensorKey(1, 1), occupied: false, _clock.Now);

        Assert.That(_vm.Filtered[0].Message, Does.Contain("clear"));
    }

    [Test]
    public void AppendSensorEdge_German_UsesGermanVerb()
    {
        LocalizationService.Instance.Apply("de");

        _vm.AppendSensorEdge("Gleis 3", new SensorKey(1, 1), occupied: true, _clock.Now);

        Assert.That(_vm.Filtered[0].Message, Does.Contain("belegt"));
    }

    [Test]
    public void AppendConnection_Simulated_AppendsSuffix()
    {
        _vm.AppendConnection(connected: true, simulated: true);

        Assert.That(_vm.Filtered[0].Kind, Is.EqualTo(LogEntryKind.Connection));
        Assert.That(_vm.Filtered[0].Message, Does.Contain("Connected"));
        Assert.That(_vm.Filtered[0].Message, Does.Contain("simulated"));
    }

    [Test]
    public void AppendConnection_Disconnected_UsesDisconnectedMessage()
    {
        _vm.AppendConnection(connected: false, simulated: false);

        Assert.That(_vm.Filtered[0].Message, Does.Contain("Disconnected"));
    }

    [Test]
    public void AppendLoco_Forward_FormatsAddressDirectionAndSpeed()
    {
        _vm.AppendLoco(new LocoSnapshot(Address: 3, Speed: 42, Forward: true));

        var message = _vm.Filtered[0].Message;
        Assert.That(_vm.Filtered[0].Kind, Is.EqualTo(LogEntryKind.Loco));
        Assert.That(message, Does.Contain("3"));
        Assert.That(message, Does.Contain("42"));
        Assert.That(message, Does.Contain("forward"));
    }

    [Test]
    public void AppendLoco_Backward_UsesBackwardWord()
    {
        _vm.AppendLoco(new LocoSnapshot(Address: 3, Speed: 0, Forward: false));

        Assert.That(_vm.Filtered[0].Message, Does.Contain("backward"));
    }

    [Test]
    public void AppendTurnout_Output2_FormatsAddressAndPosition()
    {
        _vm.AppendTurnout(new TurnoutSnapshot(Address: 5, Position: TurnoutPosition.Output2));

        var message = _vm.Filtered[0].Message;
        Assert.That(_vm.Filtered[0].Kind, Is.EqualTo(LogEntryKind.Turnout));
        Assert.That(message, Does.Contain("5"));
        Assert.That(message, Does.Contain("output 2"));
    }

    [Test]
    public void AppendTurnout_Output1_UsesOutput1Word()
    {
        _vm.AppendTurnout(new TurnoutSnapshot(Address: 5, Position: TurnoutPosition.Output1));

        Assert.That(_vm.Filtered[0].Message, Does.Contain("output 1"));
    }

    [Test]
    public void AppendTurnout_Unknown_UsesUnknownWord()
    {
        _vm.AppendTurnout(new TurnoutSnapshot(Address: 5, Position: TurnoutPosition.Unknown));

        Assert.That(_vm.Filtered[0].Message, Does.Contain("unknown"));
    }

    [Test]
    public void AppendTrackPower_On_UsesOnMessage()
    {
        _vm.AppendTrackPower(true);

        Assert.That(_vm.Filtered[0].Message, Is.EqualTo("Track power on"));
    }

    [Test]
    public void AppendTrackPower_Off_UsesOffMessage()
    {
        _vm.AppendTrackPower(false);

        Assert.That(_vm.Filtered[0].Message, Is.EqualTo("Track power off"));
    }

    [Test]
    public void Append_PutsTheNewestEntryFirst()
    {
        _vm.AppendTrackPower(true);
        _vm.AppendTrackPower(false);

        Assert.That(_vm.Filtered[0].Message, Is.EqualTo("Track power off"));
        Assert.That(_vm.Filtered[1].Message, Is.EqualTo("Track power on"));
    }

    [Test]
    public void Refilter_KeepsTheNewestEntryFirst()
    {
        _vm.AppendTrackPower(true);
        _vm.AppendTrackPower(false);

        _vm.SearchText = "power";

        Assert.That(_vm.Filtered[0].Message, Is.EqualTo("Track power off"));
        Assert.That(_vm.Filtered[1].Message, Is.EqualTo("Track power on"));
    }

    [Test]
    public void Append_BeyondCap_DropsOldest()
    {
        var vm = new TrafficLogViewModel(LocalizationService.Instance, _clock, maxEntries: 2);
        vm.StartRecording();

        vm.AppendTrackPower(true);
        vm.AppendTrackPower(false);
        vm.AppendTrackPower(true);

        Assert.That(vm.Filtered, Has.Count.EqualTo(2));
    }

    [Test]
    public void AppendMatching_RaisesEntryAppended()
    {
        var raised = 0;
        _vm.EntryAppended += (_, _) => raised++;

        _vm.AppendTrackPower(true);

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void AppendFilteredOut_DoesNotRaiseEntryAppended()
    {
        _vm.KindToggles.Single(t => t.Kind == LogEntryKind.TrackPower).IsSelected = false;
        var raised = 0;
        _vm.EntryAppended += (_, _) => raised++;

        _vm.AppendTrackPower(true);

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void ChangingFilter_DoesNotRaiseEntryAppended()
    {
        _vm.AppendTrackPower(true);
        var raised = 0;
        _vm.EntryAppended += (_, _) => raised++;

        _vm.SearchText = "power";
        _vm.KindToggles.Single(t => t.Kind == LogEntryKind.System).IsSelected = false;

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Append_WhenNotRecording_IsIgnored()
    {
        var vm = new TrafficLogViewModel(LocalizationService.Instance, _clock);

        vm.AppendTrackPower(true);

        Assert.That(vm.Filtered, Is.Empty);
        Assert.That(vm.Entries, Is.Empty);
    }

    [Test]
    public void StopRecording_IgnoresFurtherAppends()
    {
        _vm.AppendTrackPower(true);
        _vm.StopRecording();

        _vm.AppendTrackPower(false);

        Assert.That(_vm.Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public void StartRecording_ClearsPreviousEntries()
    {
        _vm.AppendTrackPower(true);

        _vm.StartRecording();

        Assert.That(_vm.Filtered, Is.Empty);
        Assert.That(_vm.Entries, Is.Empty);
    }

    [Test]
    public void LoadSession_PopulatesEntriesEvenWhenNotRecording()
    {
        var vm = new TrafficLogViewModel(LocalizationService.Instance, _clock);

        vm.LoadSession(new[] { new LogEntry(DateTimeOffset.UnixEpoch, LogEntryKind.Sensor, "Yard 3 occupied") });

        Assert.That(vm.Entries, Has.Count.EqualTo(1));
        Assert.That(vm.Filtered[0].Message, Is.EqualTo("Yard 3 occupied"));
    }

    [Test]
    public void LoadSession_ReplacesAnyExistingEntries()
    {
        _vm.AppendTrackPower(true);

        _vm.LoadSession(new[] { new LogEntry(DateTimeOffset.UnixEpoch, LogEntryKind.Sensor, "Yard 3 occupied") });

        Assert.That(_vm.Entries, Has.Count.EqualTo(1));
        Assert.That(_vm.Filtered[0].Message, Is.EqualTo("Yard 3 occupied"));
    }

    [Test]
    public void Append_BeyondCap_DropsOldestFromBacklog()
    {
        var vm = new TrafficLogViewModel(LocalizationService.Instance, _clock, maxEntries: 2);
        vm.StartRecording();

        vm.AppendTrackPower(true);
        vm.AppendTrackPower(false);
        vm.AppendTrackPower(true);

        Assert.That(vm.RecentLines(100), Has.Count.EqualTo(2));
    }

    [Test]
    public void SearchText_FiltersByMessage()
    {
        _vm.AppendSensorEdge("Yard 3", new SensorKey(1, 1), true, _clock.Now);
        _vm.AppendSensorEdge("Platform", new SensorKey(1, 2), true, _clock.Now);

        _vm.SearchText = "yard";

        Assert.That(_vm.Filtered, Has.Count.EqualTo(1));
        Assert.That(_vm.Filtered[0].Message, Does.Contain("Yard 3"));
    }

    [Test]
    public void DisablingKind_HidesThoseEntries()
    {
        _vm.AppendSystem(new SystemSnapshot(1, 1, 1, false, false, false, false, false, false));
        _vm.AppendTrackPower(true);

        _vm.KindToggles.Single(t => t.Kind == LogEntryKind.System).IsSelected = false;

        Assert.That(_vm.Filtered.Any(e => e.Kind == LogEntryKind.System), Is.False);
        Assert.That(_vm.Filtered.Any(e => e.Kind == LogEntryKind.TrackPower), Is.True);
    }

    [Test]
    public void RecentLines_ReturnsNeutralKindAndMessage()
    {
        _vm.AppendTrackPower(true);

        var lines = _vm.RecentLines(10);

        Assert.That(lines, Has.Count.EqualTo(1));
        Assert.That(lines[0].Kind, Is.EqualTo("TrackPower"));
        Assert.That(lines[0].Message, Is.Not.Empty);
    }

}
