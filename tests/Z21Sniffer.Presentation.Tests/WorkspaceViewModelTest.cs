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
    private ILogTextStore _logTextStore = null!;
    private StubClock _clock = null!;
    private string? _savePath;
    private string? _importPath;
    private string? _exportLogPath;
    private bool _openSettingsCalled;
    private WorkspaceViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = A.Fake<ICommandStationConnectionFactory>();
        _settings = A.Fake<ISettingsStore>();
        _sessionStore = A.Fake<ISessionStore>();
        _connection = A.Fake<ICommandStationConnection>();
        _logTextStore = A.Fake<ILogTextStore>();
        _clock = new StubClock();
        A.CallTo(() => _settings.Load()).Returns(new AppSettings("192.168.0.5", 21105, "en"));
        A.CallTo(() => _factory.Create(A<bool>._)).Returns(_connection);
        A.CallTo(() => _connection.IsConnected).Returns(true);
        _savePath = null;
        _importPath = null;
        _exportLogPath = null;
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
        });
        var legend = new FakeIndex<Type, IIntervalLegendDrawingStrategy>(new Dictionary<Type, IIntervalLegendDrawingStrategy>
        {
            [typeof(FeedbackSensorInterval)] = new SensorIntervalLegendDrawingStrategy(registry, new StubRemovalConfirmation()),
            [typeof(ConnectionInterval)] = new ConnectionIntervalLegendDrawingStrategy(),
            [typeof(LocoInterval)] = new LocoIntervalLegendDrawingStrategy(registry, new StubRemovalConfirmation()),
            [typeof(TrackPowerInterval)] = new TrackPowerIntervalLegendDrawingStrategy(),
        });

        return new WorkspaceViewModel(
            _factory, _settings, _sessionStore, _clock, registry, new FeedbackSensorIngest(registry), chart, legend,
            A.Fake<IMcpServerController>(),
            A.Fake<IThemeController>(),
            _logTextStore,
            post: action => action(),
            chooseSaveJsonPath: () => Task.FromResult(_savePath),
            chooseOpenJsonPath: () => Task.FromResult(_importPath),
            chooseExportLogPath: () => Task.FromResult(_exportLogPath),
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
        await StartRecording();

        _connection.LocoInfoReceived += Raise.With(_connection, new LocoSnapshot(482, 40, Forward: true, MaxSpeed: 126));

        Assert.That(_vm.Timeline.Sources.OfType<LocoIntervalSource>().Select(s => s.Address), Does.Contain(482));
    }

    [Test]
    public async Task LocoInfo_WhenNotRecording_IsIgnoredButStillLogged()
    {
        await ActivateConnection();
        _connection.ConnectionChanged += Raise.With(_connection, true);

        _connection.LocoInfoReceived += Raise.With(_connection, new LocoSnapshot(482, 40, Forward: true, MaxSpeed: 126));

        Assert.That(_vm.Timeline.Sources.OfType<LocoIntervalSource>(), Is.Empty);
        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Loco), Is.True);
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
    public async Task StartRecording_SeedsTrackPowerRow()
    {
        await StartRecording();

        Assert.That(_vm.Timeline.Sources.OfType<TrackPowerSource>().Single(), Is.Not.Null);
    }

    [Test]
    public async Task SystemStateWhileRecording_RecordsTrackPowerStatus()
    {
        await StartRecording();

        _connection.SystemStateReceived += Raise.With(_connection,
            new SystemSnapshot(0, 0, 0, ShortCircuit: true, EmergencyStop: false, TrackVoltageOff: false,
                ProgrammingMode: false, PowerLost: false, HighTemperature: false));

        var trackPower = _vm.Timeline.Sources.OfType<TrackPowerSource>().Single();
        Assert.That(trackPower.Intervals.Last().Status, Is.EqualTo(TrackPowerStatus.Short));
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
        await ActivateConnection();

        _connection.TrackPowerChanged += Raise.With(_connection, true);

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.TrackPower), Is.True);
    }

    [Test]
    public async Task ActivatedConnectionSystemState_AppendsSystemLog()
    {
        await ActivateConnection();

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
        await ActivateConnection();

        _connection.LocoInfoReceived += Raise.With(_connection, new LocoSnapshot(3, 42, true));

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Loco), Is.True);
    }

    [Test]
    public async Task ActivatedConnectionTurnout_AppendsTurnoutLog()
    {
        await ActivateConnection();

        _connection.TurnoutInfoReceived += Raise.With(_connection, new TurnoutSnapshot(5, TurnoutPosition.Output1));

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.Turnout), Is.True);
    }

    [Test]
    public async Task ConnectionChanged_AppendsConnectionLog()
    {
        await ActivateConnection();

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
        await ActivateConnection();
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
    public async Task ImportSessionCommand_WithPath_LoadsSessionIntoTimeline()
    {
        _importPath = "session.json";
        var startedAt = DateTimeOffset.UnixEpoch;
        var source = new FeedbackSensorSource { Id = "sensor:1.1", Sensor = SensorA };
        source.Apply(occupied: true, startedAt);
        A.CallTo(() => _sessionStore.LoadJson("session.json")).Returns(
            new RecordingSession(startedAt, new IIntervalSource[] { source }));

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
    public async Task ExportLogCommand_WithPath_SavesLogText()
    {
        _exportLogPath = "log.txt";

        await _vm.ExportLogCommand.ExecuteAsync(null);

        A.CallTo(() => _logTextStore.Save(A<string>._, "log.txt")).MustHaveHappened();
    }

    [Test]
    public async Task ExportLogCommand_WhenCancelled_DoesNotSave()
    {
        _exportLogPath = null;

        await _vm.ExportLogCommand.ExecuteAsync(null);

        A.CallTo(() => _logTextStore.Save(A<string>._, A<string>._)).MustNotHaveHappened();
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
