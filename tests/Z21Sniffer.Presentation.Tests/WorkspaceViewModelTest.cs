using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Logging;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class WorkspaceViewModelTest
{
    private sealed class StubClock : IClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
    }

    private static readonly SensorKey SensorA = new(1, 1);

    private ICommandStationConnectionFactory _factory = null!;
    private ISettingsStore _settings = null!;
    private ISessionStore _sessionStore = null!;
    private ICommandStationConnection _connection = null!;
    private ILogTextStore _logTextStore = null!;
    private string? _savePath;
    private string? _importPath;
    private string? _exportLogPath;
    private bool _confirmRemoveResult;
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
        A.CallTo(() => _settings.Load()).Returns(new AppSettings("192.168.0.5", 21105, "en", []));
        A.CallTo(() => _factory.Create(A<bool>._)).Returns(_connection);
        _savePath = null;
        _importPath = null;
        _exportLogPath = null;
        _confirmRemoveResult = true;
        _openSettingsCalled = false;

        _vm = Build();
    }

    private WorkspaceViewModel Build() => new(
        _factory, _settings, _sessionStore, new StubClock(), new SensorLabeler(),
        A.Fake<IMcpServerController>(),
        A.Fake<IThemeController>(),
        _logTextStore,
        post: action => action(),
        chooseSaveJsonPath: () => Task.FromResult(_savePath),
        chooseOpenJsonPath: () => Task.FromResult(_importPath),
        chooseExportLogPath: () => Task.FromResult(_exportLogPath),
        confirmRemove: _ => Task.FromResult(_confirmRemoveResult),
        openSettings: () => { _openSettingsCalled = true; return Task.CompletedTask; });

    private async Task ActivateConnection() => await _vm.Connection.ToggleConnectionCommand.ExecuteAsync(null);

    [Test]
    public async Task ActivatedConnectionFeedback_FeedsTimeline()
    {
        await ActivateConnection();

        _connection.FeedbackReceived += Raise.With(_connection, (IReadOnlyList<SensorState>)
            [new SensorState(SensorA, true)]);

        Assert.That(_vm.Timeline.Rows.Select(r => r.Sensor), Does.Contain(SensorA));
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
            new SystemSnapshot(320, 15000, 30, false, false, false, false, false));

        Assert.That(_vm.Log.Filtered.Any(e => e.Kind == LogEntryKind.System), Is.True);
    }

    [Test]
    public async Task ActivatedConnectionFeedbackEdge_AppendsSensorLog()
    {
        await ActivateConnection();

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
        A.CallTo(() => _settings.Load()).Returns(new AppSettings("192.168.0.5", 21105, "de", []));

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
            new SystemSnapshot(1, 1, 1, false, false, false, false, false));

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
        A.CallTo(() => _sessionStore.LoadJson("session.json")).Returns(
            new RecordingSession(startedAt,
                [new SensorInterval(SensorA, startedAt, startedAt + TimeSpan.FromSeconds(1))]));

        await _vm.ImportSessionCommand.ExecuteAsync(null);

        A.CallTo(() => _sessionStore.LoadJson("session.json")).MustHaveHappened();
        Assert.That(_vm.Timeline.Rows.Select(r => r.Sensor), Does.Contain(SensorA));
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
    public void ChangingMcpPort_PersistsIt()
    {
        _vm.Mcp.Port = 9999;

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.McpPort == 9999))).MustHaveHappened();
    }

    [Test]
    public async Task RemoveSensorCommand_WhenConfirmed_RemovesSensor()
    {
        _vm.Timeline.OnFeedback([new SensorState(SensorA, true)]);
        _confirmRemoveResult = true;
        var row = _vm.Timeline.Rows.Single();

        await _vm.RemoveSensorCommand.ExecuteAsync(row);

        Assert.That(_vm.Timeline.Rows, Is.Empty);
    }

    [Test]
    public async Task RemoveSensorCommand_WhenDeclined_KeepsSensor()
    {
        _vm.Timeline.OnFeedback([new SensorState(SensorA, true)]);
        _confirmRemoveResult = false;
        var row = _vm.Timeline.Rows.Single();

        await _vm.RemoveSensorCommand.ExecuteAsync(row);

        Assert.That(_vm.Timeline.Rows, Has.Count.EqualTo(1));
    }

    [Test]
    public void LoadedSensorOrder_DrivesRowInsertionOrder()
    {
        var second = new SensorKey(1, 2);
        A.CallTo(() => _settings.Load()).Returns(
            new AppSettings("192.168.0.5", 21105, "en", [], SensorOrder: [second, SensorA]));
        var vm = Build();

        vm.Timeline.OnFeedback([new SensorState(SensorA, true)]);
        vm.Timeline.OnFeedback([new SensorState(second, true)]);

        Assert.That(vm.Timeline.Rows.Select(r => r.Sensor), Is.EqualTo(new[] { second, SensorA }));
    }

    [Test]
    public void NullSavedOrder_PreservesArrivalOrder()
    {
        _vm.Timeline.OnFeedback([new SensorState(new SensorKey(1, 1), true)]);
        _vm.Timeline.OnFeedback([new SensorState(new SensorKey(0, 0), true)]);

        Assert.That(_vm.Timeline.Rows.Select(r => r.Sensor),
            Is.EqualTo(new[] { new SensorKey(1, 1), new SensorKey(0, 0) }));
    }

    [Test]
    public void ReorderingRows_PersistsSensorOrder()
    {
        _vm.Timeline.OnFeedback([new SensorState(SensorA, true), new SensorState(new SensorKey(1, 2), true)]);

        _vm.Timeline.MoveRow(0, 1);

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(
            s => s.SensorOrder != null && s.SensorOrder[0] == new SensorKey(1, 2)))).MustHaveHappened();
    }

    [Test]
    public void TogglingTheme_PersistsDarkTheme()
    {
        _vm.Theme.ToggleCommand.Execute(null);

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(s => s.DarkTheme == _vm.Theme.IsDark)))
            .MustHaveHappened();
    }

    [Test]
    public void AliasRename_PersistsAliasesToSettings()
    {
        _vm.Timeline.OnFeedback([new SensorState(SensorA, true)]);

        _vm.Timeline.Rename(SensorA, "Station track 2");

        A.CallTo(() => _settings.Save(A<AppSettings>.That.Matches(
            s => s.Aliases.Any(a => a.Name == "Station track 2")))).MustHaveHappened();
    }
}
