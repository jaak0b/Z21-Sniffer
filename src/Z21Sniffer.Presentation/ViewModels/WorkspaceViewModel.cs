using Autofac.Features.Indexed;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class WorkspaceViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly ISessionStore _sessionStore;
    private readonly ILogTextStore _logTextStore;
    private readonly FeedbackSensorIngest _ingest;
    private readonly IClock _clock;
    private readonly Action<Action> _post;
    private readonly Func<Task<string?>> _chooseSaveJsonPath;
    private readonly Func<Task<string?>> _chooseOpenJsonPath;
    private readonly Func<Task<string?>> _chooseExportLogPath;
    private readonly Func<Task> _openSettings;
    private readonly HashSet<ICommandStationConnection> _wired = new();

    private const string EnglishCode = "en";
    private const string GermanCode = "de";

    [ObservableProperty]
    private AppLanguage _selectedLanguage;

    public WorkspaceViewModel(
        ICommandStationConnectionFactory factory,
        ISettingsStore settings,
        ISessionStore sessionStore,
        IClock clock,
        IIntervalSourceRegistry registry,
        FeedbackSensorIngest ingest,
        IIndex<Type, IIntervalChartDrawingStrategy> chartStrategies,
        IIndex<Type, IIntervalLegendDrawingStrategy> legendStrategies,
        IMcpServerController mcpController,
        IThemeController themeController,
        ILogTextStore logTextStore,
        Action<Action> post,
        Func<Task<string?>> chooseSaveJsonPath,
        Func<Task<string?>> chooseOpenJsonPath,
        Func<Task<string?>> chooseExportLogPath,
        Func<Task> openSettings)
    {
        _settings = settings;
        _sessionStore = sessionStore;
        _logTextStore = logTextStore;
        _ingest = ingest;
        _clock = clock;
        _post = post;
        _chooseSaveJsonPath = chooseSaveJsonPath;
        _chooseOpenJsonPath = chooseOpenJsonPath;
        _chooseExportLogPath = chooseExportLogPath;
        _openSettings = openSettings;

        var loaded = settings.Load();
        Localization.Apply(loaded.Language);
        _selectedLanguage = loaded.Language == GermanCode ? AppLanguage.German : AppLanguage.English;

        Connection = new ConnectionViewModel(factory, settings);
        Timeline = new TimelineViewModel(registry, chartStrategies, legendStrategies, clock);
        Log = new TrafficLogViewModel(Localization, clock);
        Mcp = new McpServerViewModel(mcpController, loaded.McpPort);
        Theme = new ThemeViewModel(themeController, loaded.DarkTheme);

        _ingest.EdgeDetected += (_, e) => Log.AppendSensorEdge(e.Label, e.Sensor, e.Occupied, e.At);
        Connection.ConnectionActivated += Wire;
        Theme.Changed += (_, _) =>
            _settings.Save(_settings.Load() with { DarkTheme = Theme.IsDark });
        Mcp.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(McpServerViewModel.Port))
                _settings.Save(_settings.Load() with { McpPort = Mcp.Port });
        };
    }

    public LocalizationService Localization => LocalizationService.Instance;

    public IReadOnlyList<AppLanguage> Languages { get; } = [AppLanguage.English, AppLanguage.German];

    public ConnectionViewModel Connection { get; }

    public TimelineViewModel Timeline { get; }

    public TrafficLogViewModel Log { get; }

    public McpServerViewModel Mcp { get; }

    public ThemeViewModel Theme { get; }

    private void Wire(ICommandStationConnection connection)
    {
        if (!_wired.Add(connection)) return;

        connection.FeedbackReceived += (_, states) => _post(() => _ingest.Apply(states, _clock.Now));
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

    partial void OnSelectedLanguageChanged(AppLanguage value) =>
        SetLanguage(value == AppLanguage.German ? GermanCode : EnglishCode);

    [RelayCommand]
    private void SetLanguage(string code)
    {
        Localization.Apply(code);
        _settings.Save(_settings.Load() with { Language = code });
    }
}
