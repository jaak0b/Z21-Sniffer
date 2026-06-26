using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class WorkspaceViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly ISessionStore _sessionStore;
    private readonly ILogTextStore _logTextStore;
    private readonly Action<Action> _post;
    private readonly Func<Task<string?>> _chooseSaveJsonPath;
    private readonly Func<Task<string?>> _chooseOpenJsonPath;
    private readonly Func<Task<string?>> _chooseExportLogPath;
    private readonly Func<SensorRowViewModel, Task<bool>> _confirmRemove;
    private readonly Func<Task> _openSettings;
    private readonly HashSet<ICommandStationConnection> _wired = new();

    public WorkspaceViewModel(
        ICommandStationConnectionFactory factory,
        ISettingsStore settings,
        ISessionStore sessionStore,
        IClock clock,
        SensorLabeler labeler,
        IMcpServerController mcpController,
        IThemeController themeController,
        ILogTextStore logTextStore,
        Action<Action> post,
        Func<Task<string?>> chooseSaveJsonPath,
        Func<Task<string?>> chooseOpenJsonPath,
        Func<Task<string?>> chooseExportLogPath,
        Func<SensorRowViewModel, Task<bool>> confirmRemove,
        Func<Task> openSettings)
    {
        _settings = settings;
        _sessionStore = sessionStore;
        _logTextStore = logTextStore;
        _post = post;
        _chooseSaveJsonPath = chooseSaveJsonPath;
        _chooseOpenJsonPath = chooseOpenJsonPath;
        _chooseExportLogPath = chooseExportLogPath;
        _confirmRemove = confirmRemove;
        _openSettings = openSettings;

        var loaded = settings.Load();
        Localization.Apply(loaded.Language);

        Connection = new ConnectionViewModel(factory, settings);
        Timeline = new TimelineViewModel(new FeedbackRecorder(clock), labeler, clock, loaded.Aliases, loaded.SensorOrder ?? []);
        Log = new TrafficLogViewModel(Localization, clock);
        Mcp = new McpServerViewModel(mcpController, loaded.McpPort);
        Theme = new ThemeViewModel(themeController, loaded.DarkTheme);

        Timeline.SensorEdgeDetected += (_, e) => Log.AppendSensorEdge(e.Label, e.Sensor, e.Occupied, e.At);
        Connection.ConnectionActivated += Wire;
        Timeline.AliasesChanged += (_, _) =>
            _settings.Save(_settings.Load() with { Aliases = Timeline.Aliases });
        Timeline.RowsReordered += (_, _) =>
            _settings.Save(_settings.Load() with { SensorOrder = Timeline.Order });
        Theme.Changed += (_, _) =>
            _settings.Save(_settings.Load() with { DarkTheme = Theme.IsDark });
        Mcp.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(McpServerViewModel.Port))
                _settings.Save(_settings.Load() with { McpPort = Mcp.Port });
        };
    }

    public LocalizationService Localization => LocalizationService.Instance;

    public ConnectionViewModel Connection { get; }

    public TimelineViewModel Timeline { get; }

    public TrafficLogViewModel Log { get; }

    public McpServerViewModel Mcp { get; }

    public ThemeViewModel Theme { get; }

    [RelayCommand]
    private async Task RemoveSensor(SensorRowViewModel row)
    {
        if (await _confirmRemove(row)) Timeline.RemoveSensor(row.Sensor);
    }

    private void Wire(ICommandStationConnection connection)
    {
        if (!_wired.Add(connection)) return;

        connection.FeedbackReceived += (_, states) => _post(() => Timeline.OnFeedback(states));
        connection.ConnectionChanged += (_, connected) => _post(() =>
        {
            Connection.IsConnected = connected;
            Log.AppendConnection(connected, Connection.IsSimulated);
        });
        connection.TrackPowerChanged += (_, on) => _post(() =>
        {
            Connection.TrackPowerOn = on;
            Log.AppendTrackPower(on);
        });
        connection.SystemStateReceived += (_, snapshot) => _post(() => Log.AppendSystem(snapshot));
        connection.LocoInfoReceived += (_, loco) => _post(() => Log.AppendLoco(loco));
        connection.TurnoutInfoReceived += (_, turnout) => _post(() => Log.AppendTurnout(turnout));
    }

    [RelayCommand]
    private async Task SaveSession()
    {
        var path = await _chooseSaveJsonPath();
        if (string.IsNullOrEmpty(path)) return;
        _sessionStore.SaveJson(Timeline.ToSession(), path);
    }

    [RelayCommand]
    private async Task ImportSession()
    {
        var path = await _chooseOpenJsonPath();
        if (string.IsNullOrEmpty(path)) return;
        Timeline.LoadSession(_sessionStore.LoadJson(path));
    }

    [RelayCommand]
    private Task OpenSettings() => _openSettings();

    [RelayCommand]
    private async Task ExportLog()
    {
        var path = await _chooseExportLogPath();
        if (string.IsNullOrEmpty(path)) return;
        _logTextStore.Save(Log.BuildExportText(), path);
    }

    [RelayCommand]
    private void SetLanguage(string code)
    {
        Localization.Apply(code);
        _settings.Save(_settings.Load() with { Language = code });
    }
}
