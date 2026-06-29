using Autofac.Features.Indexed;
using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Logging;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class WorkspaceViewModelTest
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

    private ICommandStationConnectionFactory _factory = null!;
    private ISettingsStore _settings = null!;
    private ISessionStore _sessionStore = null!;
    private ICommandStationConnection _connection = null!;
    private IStationCurrentLimits _currentLimits = null!;
    private StubClock _clock = null!;
    private string? _savePath;
    private string? _importPath;
    private bool _openSettingsCalled;
    private WorkspaceViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = A.Fake<ICommandStationConnectionFactory>();
        _settings = A.Fake<ISettingsStore>();
        _sessionStore = A.Fake<ISessionStore>();
        _connection = A.Fake<ICommandStationConnection>();
        _currentLimits = A.Fake<IStationCurrentLimits>();
        _clock = new StubClock();
        A.CallTo(() => _settings.Load()).Returns(new AppSettings("192.168.0.5", 21105, "en"));
        A.CallTo(() => _factory.Create(A<bool>._)).Returns(_connection);
        A.CallTo(() => _connection.IsConnected).Returns(true);
        _savePath = null;
        _importPath = null;
        _openSettingsCalled = false;

        _vm = Build();
    }

    private WorkspaceViewModel Build()
    {
        var registry = new IntervalSourceRegistry();
        var chart = new FakeIndex<Type, IIntervalChartDrawingStrategy>(new Dictionary<Type, IIntervalChartDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalChartDrawingStrategy(),
            [typeof(ConnectionInterval)] = new ConnectionIntervalChartDrawingStrategy(),
            [typeof(LocoInterval)] = new LocoIntervalChartDrawingStrategy(),
            [typeof(TrackPowerInterval)] = new TrackPowerIntervalChartDrawingStrategy(),
            [typeof(SystemCurrentInterval)] = new SystemCurrentIntervalChartDrawingStrategy(),
            [typeof(AccessoryInterval)] = new AccessoryIntervalChartDrawingStrategy(),
        });
        var legend = new FakeIndex<Type, IIntervalLegendDrawingStrategy>(new Dictionary<Type, IIntervalLegendDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalLegendDrawingStrategy(registry, new StubRemovalConfirmation()),
            [typeof(ConnectionInterval)] = new ConnectionIntervalLegendDrawingStrategy(),
            [typeof(LocoInterval)] = new LocoIntervalLegendDrawingStrategy(registry, new StubRemovalConfirmation()),
            [typeof(TrackPowerInterval)] = new TrackPowerIntervalLegendDrawingStrategy(),
            [typeof(SystemCurrentInterval)] = new SystemCurrentIntervalLegendDrawingStrategy(),
            [typeof(AccessoryInterval)] = new AccessoryIntervalLegendDrawingStrategy(registry, new StubRemovalConfirmation()),
        });

        return new WorkspaceViewModel(
            _factory, _settings, _sessionStore, _clock, registry, new FeedbackSensorIngest(registry), chart, legend,
            A.Fake<IMcpServerController>(),
            A.Fake<IThemeController>(),
            _currentLimits,
            post: action => action(),
            chooseSaveJsonPath: () => Task.FromResult(_savePath),
            chooseOpenJsonPath: () => Task.FromResult(_importPath),
            openSettings: () => { _openSettingsCalled = true; return Task.CompletedTask; });
    }

    private async Task ActivateConnection() => await _vm.Connection.ToggleConnectionCommand.ExecuteAsync(null);

    private async Task StartRecording()
    {
        await ActivateConnection();
        _connection.ConnectionChanged += Raise.With(_connection, true);
        _vm.Recording.ToggleCommand.Execute(null);
    }

    [Test]
    public async Task ActivatedConnectionFeedback_WhileRecording_FeedsTimeline()
    {
        await StartRecording();

        _connection.FeedbackReceived += Raise.With(_connection, (IReadOnlyList<SensorState>)
            [new SensorState(SensorA, true)]);

        Assert.That(_vm.Timeline.Sources.OfType<FeedbackSensorSource>().Select(s => s.Sensor), Does.Contain(SensorA));
    }

    [Test]
    public async Task LocoInfo_WhileRecording_FeedsTimeline()
    {
        _vm.CaptureTrainData = true;
        await StartRecording();

        _connection.LocoInfoReceived += Raise.With(_connection, new LocoSnapshot(482, 40, Forward: true, MaxSpeed: 126));

        Assert.That(_vm.Timeline.Sources.OfType<LocoIntervalSource>().Select(s => s.Address), Does.Contain(482));
    }

    [Test]
    public async Task LocoInfo_WhenNotRecording_IsNeitherTimelineFedNorLogged()
    {
        _vm.CaptureTrainData = true;
        await ActivateConnection();
        _connection.ConnectionChanged += Raise.With(_connection, true);

        _connection.LocoInfoReceived += Raise.With(_connection, new LocoSnapshot(482, 40, Forward: true, MaxSpeed: 126));

        Assert.That(_vm.Timeline.Sources.OfType<LocoIntervalSource>(), Is.Empty);
        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Loco), Is.False);
    }

    [Test]
    public async Task LocoInfo_WhenCaptureTrainDataOff_FeedsNeitherTimelineNorLog()
    {
        await StartRecording();

        _connection.LocoInfoReceived += Raise.With(_connection, new LocoSnapshot(482, 40, Forward: true, MaxSpeed: 126));

        Assert.That(_vm.Timeline.Sources.OfType<LocoIntervalSource>(), Is.Empty);
        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Loco), Is.False);
    }

    [Test]
    public void Constructor_AppliesLoadedCaptureTrainData()
    {
        A.CallTo(() => _settings.Load()).Returns(
            new AppSettings("192.168.0.5", 21105, "en") with { CaptureTrainData = true });

        var vm = Build();

        Assert.That(vm.CaptureTrainData, Is.True);
    }

    [Test]
    public void SettingCaptureTrainData_PersistsIt()
    {
        _vm.CaptureTrainData = true;

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.CaptureTrainData))).MustHaveHappened();
    }

    [Test]
    public async Task Feedback_WhenNotRecording_IsIgnored()
    {
        await ActivateConnection();
        _connection.ConnectionChanged += Raise.With(_connection, true);

        _connection.FeedbackReceived += Raise.With(_connection, (IReadOnlyList<SensorState>)
            [new SensorState(SensorA, true)]);

        Assert.That(_vm.Timeline.Sources.OfType<FeedbackSensorSource>(), Is.Empty);
    }

    [Test]
    public async Task ConnectionLostWhileRecording_KeepsRecordingAndRecordsDisconnectedInterval()
    {
        await StartRecording();

        _connection.ConnectionChanged += Raise.With(_connection, false);

        Assert.That(_vm.Recording.IsRecording, Is.True);
        var connection = _vm.Timeline.Sources.OfType<ConnectionSource>().Single();
        Assert.That(connection.Intervals.Last().Connected, Is.False);
    }

    [Test]
    public async Task ConnectionChanged_WhenNotRecording_DoesNotCreateConnectionSource()
    {
        await ActivateConnection();

        _connection.ConnectionChanged += Raise.With(_connection, true);

        Assert.That(_vm.Timeline.Sources.OfType<ConnectionSource>(), Is.Empty);
    }

    [Test]
    public async Task RestartingRecording_CollapsesTimelineScrollbackToTheNewStart()
    {
        await StartRecording();
        _clock.Now = DateTimeOffset.UnixEpoch.AddSeconds(120);
        _vm.Timeline.Tick();
        Assert.That(_vm.Timeline.ScrollMaxSeconds, Is.GreaterThan(0));

        _vm.Recording.ToggleCommand.Execute(null);
        _vm.Recording.ToggleCommand.Execute(null);
        _vm.Timeline.Tick();

        Assert.That(_vm.Timeline.ScrollMaxSeconds, Is.LessThan(1));
        Assert.That(_vm.Timeline.StartedAt, Is.EqualTo(_clock.Now));
    }

    [Test]
    public async Task StartRecording_RequestsCurrentStateFromConnection()
    {
        await StartRecording();

        A.CallTo(() => _connection.RequestCurrentStateAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void StartRecording_WhenDisconnected_DoesNotThrowOrRequestState()
    {
        _vm.Recording.ToggleCommand.Execute(null);

        Assert.That(_vm.Recording.IsRecording, Is.True);
        A.CallTo(() => _connection.RequestCurrentStateAsync()).MustNotHaveHappened();
    }

    [Test]
    public async Task StartRecording_DoesNotInventTrackPowerRowBeforeAnyStationReply()
    {
        await StartRecording();

        Assert.That(_vm.Timeline.Sources.OfType<TrackPowerSource>(), Is.Empty);
    }

    [Test]
    public async Task SystemStateWhileRecording_RecordsTrackPowerStatus()
    {
        await StartRecording();

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(0, 0, 0, ShortCircuit: true, EmergencyStop: false, TrackVoltageOff: false,
                ProgrammingMode: false, PowerLost: false, HighTemperature: false));

        var trackPower = _vm.Timeline.Sources.OfType<TrackPowerSource>().Single();
        Assert.That(trackPower.Id, Is.EqualTo("trackpower"));
        Assert.That(trackPower.Intervals.Last().Status, Is.EqualTo(TrackPowerStatus.Short));
    }

    [Test]
    public async Task SystemStateWhileRecording_RecordsSystemCurrentWithTheResolvedDeviceNameAndLimit()
    {
        A.CallTo(() => _currentLimits.Lookup(A<StationHardware>.That.Matches(h => h.TypeCode == 529)))
            .Returns(new StationCurrentLimit("Z21 XL", 6000));
        await StartRecording();
        _connection.HardwareInfoReceived += Raise.With(_connection, new StationHardware(529, 0));

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(1234, 0, 0, false, false, false, false, false, false));

        var source = _vm.Timeline.Sources.OfType<SystemCurrentSource>().Single();
        Assert.That(source.Id, Is.EqualTo("systemcurrent"));
        var interval = source.Intervals.Last();
        Assert.That(interval.TypeCode, Is.EqualTo(529));
        Assert.That(interval.DeviceName, Is.EqualTo("Z21 XL"));
        Assert.That(interval.MaxCurrentMilliamps, Is.EqualTo(6000));
        Assert.That(interval.Samples.Last().Milliamps, Is.EqualTo(1234));
    }

    [Test]
    public async Task SystemStateWhileRecording_ForAnUnknownDevice_RecordsWithNullNameAndMax()
    {
        A.CallTo(() => _currentLimits.Lookup(A<StationHardware>._)).Returns(null);
        await StartRecording();
        _connection.HardwareInfoReceived += Raise.With(_connection, new StationHardware(99999, 0));

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(500, 0, 0, false, false, false, false, false, false));

        var interval = _vm.Timeline.Sources.OfType<SystemCurrentSource>().Single().Intervals.Last();
        Assert.That(interval.TypeCode, Is.EqualTo(99999));
        Assert.That(interval.DeviceName, Is.Null);
        Assert.That(interval.MaxCurrentMilliamps, Is.Null);
    }

    [Test]
    public async Task SystemState_WhenNotRecording_DoesNotCreateSystemCurrentSource()
    {
        await ActivateConnection();

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(500, 0, 0, false, false, false, false, false, false));

        Assert.That(_vm.Timeline.Sources.OfType<SystemCurrentSource>(), Is.Empty);
    }

    [Test]
    public async Task SystemState_WhenNotRecording_DoesNotCreateTrackPowerSource()
    {
        await ActivateConnection();

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(0, 0, 0, false, false, false, false, false, false));

        Assert.That(_vm.Timeline.Sources.OfType<TrackPowerSource>(), Is.Empty);
    }

    [Test]
    public async Task SensorEdgeLog_UsesWallClock()
    {
        await StartRecording();
        _clock.Now = DateTimeOffset.UnixEpoch.AddMinutes(3);

        _connection.FeedbackReceived += Raise.With(_connection, (IReadOnlyList<SensorState>)
            [new SensorState(SensorA, true)]);

        var entry = _vm.Log.Filtered.Single(e => e.Kind == LogEntryKind.Sensor);
        Assert.That(entry.Timestamp, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void SelectedLanguage_SetToEnglishAfterGerman_AppliesEnglish()
    {
        _vm.SelectedLanguage = AppLanguage.German;

        _vm.SelectedLanguage = AppLanguage.English;

        Assert.That(_vm.Localization.CurrentCode, Is.EqualTo("en"));
    }

    [Test]
    public void TimelineClock_IsFrozenWhenNotRecording()
    {
        var before = _vm.TimelineClock.Now;
        _clock.Now = _clock.Now.AddSeconds(30);

        Assert.That(_vm.TimelineClock.Now, Is.EqualTo(before));
    }

    [Test]
    public async Task TimelineClock_TracksWallClockWhileRecording()
    {
        await StartRecording();
        _clock.Now = DateTimeOffset.UnixEpoch.AddMinutes(2);

        Assert.That(_vm.TimelineClock.Now, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void Timeline_DoesNotAdvanceWhenNotRecording()
    {
        _vm.Timeline.Tick();
        var before = _vm.Timeline.ViewportEnd;
        _clock.Now = _clock.Now.AddSeconds(30);

        _vm.Timeline.Tick();

        Assert.That(_vm.Timeline.ViewportEnd, Is.EqualTo(before));
    }

    [Test]
    public async Task Timeline_AdvancesWhileRecording()
    {
        await StartRecording();
        _vm.Timeline.Tick();
        var before = _vm.Timeline.ViewportEnd;
        _clock.Now = _clock.Now.AddSeconds(30);

        _vm.Timeline.Tick();

        Assert.That(_vm.Timeline.ViewportEnd, Is.GreaterThan(before));
    }

    [Test]
    public async Task ActivatedConnectionTrackPower_AppendsLog()
    {
        await StartRecording();

        _connection.TrackPowerChanged += Raise.With(_connection, true);

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.TrackPower), Is.True);
    }

    [Test]
    public async Task ActivatedConnectionSystemState_AppendsSystemLog()
    {
        await StartRecording();

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(320, 15000, 30, false, false, false, false, false, false));

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.System), Is.True);
    }

    [Test]
    public async Task ActivatedConnectionFeedbackEdge_AppendsSensorLog()
    {
        await StartRecording();

        _connection.FeedbackReceived += Raise.With(_connection, (IReadOnlyList<SensorState>)
            [new SensorState(SensorA, true)]);

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Sensor), Is.True);
    }

    [Test]
    public async Task ActivatedConnectionLoco_AppendsLocoLog()
    {
        _vm.CaptureTrainData = true;
        await StartRecording();

        _connection.LocoInfoReceived += Raise.With(_connection, new LocoSnapshot(3, 42, true));

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Loco), Is.True);
    }

    [Test]
    public async Task ActivatedConnectionTurnout_AppendsTurnoutLog()
    {
        await StartRecording();

        _connection.TurnoutInfoReceived += Raise.With(_connection, new TurnoutSnapshot(5, TurnoutPosition.Output1));

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Turnout), Is.True);
    }

    [Test]
    public async Task ResetAliasesCommand_RestoresDefaultLabels()
    {
        await StartRecording();
        _connection.FeedbackReceived += Raise.With(_connection, (IReadOnlyList<SensorState>)
            [new SensorState(SensorA, true)]);
        var sensor = _vm.Timeline.Sources.OfType<FeedbackSensorSource>().Single();
        sensor.Label = "Yard 3";

        _vm.ResetAliasesCommand.Execute(null);

        Assert.That(sensor.Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public async Task ResetOrderCommand_RevertsToCreationOrder()
    {
        await StartRecording();
        _connection.FeedbackReceived += Raise.With(_connection, (IReadOnlyList<SensorState>)
            [new SensorState(new SensorKey(1, 1), true), new SensorState(new SensorKey(1, 2), true)]);
        var ids = _vm.Timeline.Sources.OfType<FeedbackSensorSource>().Select(s => s.Id).ToList();
        _vm.Timeline.MoveRow(0, 1);

        _vm.ResetOrderCommand.Execute(null);

        Assert.That(_vm.Timeline.Sources.OfType<FeedbackSensorSource>().Select(s => s.Id), Is.EqualTo(ids));
    }

    [Test]
    public async Task ActivatedConnectionTurnout_WhileRecording_FeedsTimeline()
    {
        await StartRecording();

        _connection.TurnoutInfoReceived += Raise.With(_connection, new TurnoutSnapshot(7, TurnoutPosition.Output1));

        var accessory = _vm.Timeline.Sources.OfType<AccessorySource>().Single();
        Assert.That(accessory.Address, Is.EqualTo(7));
        Assert.That(accessory.Intervals, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ActivatedConnectionTurnout_WhileNotRecording_DoesNotFeedTimeline()
    {
        await ActivateConnection();

        _connection.TurnoutInfoReceived += Raise.With(_connection, new TurnoutSnapshot(7, TurnoutPosition.Output1));

        Assert.That(_vm.Timeline.Sources.OfType<AccessorySource>(), Is.Empty);
    }

    [Test]
    public async Task ActivatingTheSameConnectionTwice_WiresItOnlyOnce()
    {
        await ActivateConnection();
        await ActivateConnection();
        _connection.ConnectionChanged += Raise.With(_connection, true);
        _vm.Recording.ToggleCommand.Execute(null);

        _connection.TurnoutInfoReceived += Raise.With(_connection, new TurnoutSnapshot(7, TurnoutPosition.Output1));

        var accessory = _vm.Timeline.Sources.OfType<AccessorySource>().Single();
        Assert.That(accessory.Intervals, Has.Count.EqualTo(1), "a double-wired connection would record the same turnout twice");
    }

    [Test]
    public async Task ConnectionChanged_AppendsConnectionLog()
    {
        await StartRecording();

        _connection.ConnectionChanged += Raise.With(_connection, true);

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Connection), Is.True);
    }

    [Test]
    public async Task TrackPowerChanged_UpdatesConnectionTrackPowerOn()
    {
        await ActivateConnection();

        _connection.TrackPowerChanged += Raise.With(_connection, true);

        Assert.That(_vm.Connection.TrackPowerOn, Is.True);
    }

    [Test]
    public void Constructor_AppliesLoadedLanguage()
    {
        A.CallTo(() => _settings.Load()).Returns(new AppSettings("192.168.0.5", 21105, "de"));

        var vm = Build();

        Assert.That(vm.Localization.CurrentCode, Is.EqualTo("de"));
        vm.Localization.Apply("en");
    }

    [Test]
    public async Task ConnectionChanged_UpdatesConnectionIsConnected()
    {
        await ActivateConnection();

        _connection.ConnectionChanged += Raise.With(_connection, true);

        Assert.That(_vm.Connection.IsConnected, Is.True);
    }

    [Test]
    public async Task ReconnectingSameConnection_DoesNotDoubleWireEvents()
    {
        await StartRecording();
        await ActivateConnection();

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(1, 1, 1, false, false, false, false, false, false));

        Assert.That(_vm.Log.Filtered.Count(e => e.Kind == LogEntryKind.System), Is.EqualTo(1));
    }

    [Test]
    public async Task SaveSessionCommand_WithPath_SavesSession()
    {
        _savePath = "session.json";

        await _vm.SaveSessionCommand.ExecuteAsync(null);

        A.CallTo(() => _sessionStore.SaveJson(A<RecordingSession>._, "session.json")).MustHaveHappened();
    }

    [Test]
    public async Task SaveSessionCommand_WhenCancelled_DoesNotSave()
    {
        _savePath = null;

        await _vm.SaveSessionCommand.ExecuteAsync(null);

        A.CallTo(() => _sessionStore.SaveJson(A<RecordingSession>._, A<string>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task SaveSessionCommand_IncludesTheRecordedTrafficLog()
    {
        _savePath = "session.json";
        await StartRecording();
        _connection.TrackPowerChanged += Raise.With(_connection, true);
        RecordingSession? saved = null;
        A.CallTo(() => _sessionStore.SaveJson(A<RecordingSession>._, "session.json"))
            .Invokes((RecordingSession s, string _) => saved = s);

        await _vm.SaveSessionCommand.ExecuteAsync(null);

        Assert.That(saved!.TrafficLog, Is.Not.Null);
        Assert.That(saved.TrafficLog!.Any(e => e.Kind == LogEntryKind.TrackPower), Is.True);
    }

    [Test]
    public async Task StoppingRecording_StopsLoggingFurtherEvents()
    {
        await StartRecording();
        _vm.Recording.ToggleCommand.Execute(null);

        _connection.TrackPowerChanged += Raise.With(_connection, true);

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.TrackPower), Is.False);
    }

    [Test]
    public async Task RestartingRecording_ClearsThePreviousLog()
    {
        await StartRecording();
        _connection.TrackPowerChanged += Raise.With(_connection, true);
        _vm.Recording.ToggleCommand.Execute(null);

        _vm.Recording.ToggleCommand.Execute(null);

        Assert.That(_vm.Log.Filtered, Is.Empty);
    }

    [Test]
    public async Task ImportSessionCommand_LoadsTheTrafficLogIntoTheLogView()
    {
        _importPath = "session.json";
        var entry = new LogEntry(DateTimeOffset.UnixEpoch, LogEntryKind.Sensor, "Yard 3 occupied");
        A.CallTo(() => _sessionStore.LoadJson("session.json")).Returns(
            new RecordingSession(DateTimeOffset.UnixEpoch, Array.Empty<IIntervalSource>(), new[] { entry }));

        await _vm.ImportSessionCommand.ExecuteAsync(null);

        Assert.That(_vm.Log.Entries.Select(e => e.Message), Does.Contain("Yard 3 occupied"));
    }

    [Test]
    public void ImportSessionCommand_WhenNotRecording_CanExecute() =>
        Assert.That(_vm.ImportSessionCommand.CanExecute(null), Is.True);

    [Test]
    public async Task ImportSessionCommand_WhileRecording_CannotExecute()
    {
        await StartRecording();

        Assert.That(_vm.ImportSessionCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task ImportSessionCommand_BecomesExecutableAgainAfterRecordingStops()
    {
        await StartRecording();
        _vm.Recording.ToggleCommand.Execute(null);

        Assert.That(_vm.ImportSessionCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task TogglingRecording_RefreshesImportSessionCanExecute()
    {
        var raised = 0;
        _vm.ImportSessionCommand.CanExecuteChanged += (_, _) => raised++;

        await StartRecording();

        Assert.That(raised, Is.GreaterThan(0));
    }

    [Test]
    public async Task ImportSessionCommand_WithNoTrafficLog_LoadsAnEmptyLogWithoutThrowing()
    {
        _importPath = "session.json";
        A.CallTo(() => _sessionStore.LoadJson("session.json")).Returns(
            new RecordingSession(DateTimeOffset.UnixEpoch, Array.Empty<IIntervalSource>(), null));

        await _vm.ImportSessionCommand.ExecuteAsync(null);

        Assert.That(_vm.Log.Entries, Is.Empty);
    }

    [Test]
    public async Task ImportSessionCommand_WithPath_LoadsSessionIntoTimeline()
    {
        _importPath = "session.json";
        var startedAt = DateTimeOffset.UnixEpoch;
        var source = new FeedbackSensorSource { Id = "sensor:1.1", Sensor = SensorA };
        source.Apply(occupied: true, startedAt);
        A.CallTo(() => _sessionStore.LoadJson("session.json")).Returns(
            new RecordingSession(startedAt, new IIntervalSource[] { source }, Array.Empty<LogEntry>()));

        await _vm.ImportSessionCommand.ExecuteAsync(null);

        A.CallTo(() => _sessionStore.LoadJson("session.json")).MustHaveHappened();
        Assert.That(_vm.Timeline.Sources.OfType<FeedbackSensorSource>().Select(s => s.Sensor), Does.Contain(SensorA));
    }

    [Test]
    public async Task ImportSessionCommand_WhenCancelled_DoesNotLoad()
    {
        _importPath = null;

        await _vm.ImportSessionCommand.ExecuteAsync(null);

        A.CallTo(() => _sessionStore.LoadJson(A<string>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task OpenSettingsCommand_InvokesCallback()
    {
        await _vm.OpenSettingsCommand.ExecuteAsync(null);

        Assert.That(_openSettingsCalled, Is.True);
    }

    [Test]
    public void SetLanguageCommand_AppliesAndPersistsLanguage()
    {
        _vm.SetLanguageCommand.Execute("de");

        Assert.That(_vm.Localization.CurrentCode, Is.EqualTo("de"));
        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.Language == "de"))).MustHaveHappened();
        _vm.Localization.Apply("en");
    }

    [Test]
    public void SelectedLanguage_English_WhenLoadedEnglish()
    {
        Assert.That(_vm.SelectedLanguage, Is.EqualTo(AppLanguage.English));
    }

    [Test]
    public void SelectedLanguage_German_WhenLoadedGerman()
    {
        A.CallTo(() => _settings.Load()).Returns(new AppSettings("192.168.0.5", 21105, "de"));

        var vm = Build();

        Assert.That(vm.SelectedLanguage, Is.EqualTo(AppLanguage.German));
        vm.Localization.Apply("en");
    }

    [Test]
    public void SelectedLanguage_SetToGerman_AppliesAndPersists()
    {
        _vm.SelectedLanguage = AppLanguage.German;

        Assert.That(_vm.Localization.CurrentCode, Is.EqualTo("de"));
        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.Language == "de"))).MustHaveHappened();
        _vm.Localization.Apply("en");
    }

    [Test]
    public void ChangingMcpPort_PersistsIt()
    {
        _vm.Mcp.Port = 9999;

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.McpPort == 9999))).MustHaveHappened();
    }

    [Test]
    public void TogglingTheme_PersistsDarkTheme()
    {
        _vm.Theme.ToggleCommand.Execute(null);

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.DarkTheme == _vm.Theme.IsDark)))
            .MustHaveHappened();
    }
}
