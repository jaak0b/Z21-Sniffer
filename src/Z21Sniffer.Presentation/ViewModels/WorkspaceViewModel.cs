using Autofac.Features.Indexed;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class WorkspaceViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly ISessionStore _sessionStore;
    private readonly FeedbackSensorIngest _ingest;
    private readonly LocoIngest _locoIngest;
    private readonly IIntervalSourceRegistry _registry;
    private readonly IClock _clock;
    private readonly RecordingClock _recordingClock;
    private readonly IStationCurrentLimits _stationCurrentLimits;
    private readonly Action<Action> _post;
    private StationHardware _hardware = new(TypeCode: 0, FirmwareVersion: 0);
    private StationCurrentLimit? _stationLimit;
    private readonly Func<Task<string?>> _chooseSaveJsonPath;
    private readonly Func<Task<string?>> _chooseOpenJsonPath;
    private readonly Func<Task> _openSettings;
    private readonly HashSet<ICommandStationConnection> _wired = new();

    private const string EnglishCode = "en";
    private const string GermanCode = "de";

    [ObservableProperty]
    private AppLanguage _selectedLanguage;

    [ObservableProperty]
    private bool _captureTrainData;

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
        IStationCurrentLimits stationCurrentLimits,
        Action<Action> post,
        Func<Task<string?>> chooseSaveJsonPath,
        Func<Task<string?>> chooseOpenJsonPath,
        Func<Task> openSettings)
    {
        _settings = settings;
        _sessionStore = sessionStore;
        _stationCurrentLimits = stationCurrentLimits;
        _ingest = ingest;
        _locoIngest = new LocoIngest(registry);
        _registry = registry;
        _clock = clock;
        _recordingClock = new RecordingClock(clock);
        _post = post;
        _chooseSaveJsonPath = chooseSaveJsonPath;
        _chooseOpenJsonPath = chooseOpenJsonPath;
        _openSettings = openSettings;

        var loaded = settings.Load();
        Localization.Apply(loaded.Language);
        _selectedLanguage = loaded.Language == GermanCode ? AppLanguage.German : AppLanguage.English;
        _captureTrainData = loaded.CaptureTrainData;

        Connection = new ConnectionViewModel(factory, settings);
        Timeline = new TimelineViewModel(registry, chartStrategies, legendStrategies, _recordingClock);
        Recording = new RecordingViewModel(registry, _recordingClock, () => Connection.IsConnected);
        Log = new TrafficLogViewModel(Localization, clock);
        Mcp = new McpServerViewModel(mcpController, loaded.McpPort);
        Theme = new ThemeViewModel(themeController, loaded.DarkTheme);

        _ingest.EdgeDetected += (_, e) => Log.AppendSensorEdge(e.Label, e.Sensor, e.Occupied, _clock.Now);
        Connection.ConnectionActivated += Wire;
        Theme.Changed += (_, _) =>
            _settings.Save(_settings.Load() with { DarkTheme = Theme.IsDark });
        Mcp.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(McpServerViewModel.Port))
                _settings.Save(_settings.Load() with { McpPort = Mcp.Port });
        };
        Recording.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(RecordingViewModel.IsRecording)) return;
            if (Recording.IsRecording)
            {
                Log.StartRecording();
                Timeline.BeginSession();
                _ = Connection.RequestCurrentStateAsync();
            }
            else
            {
                Log.StopRecording();
            }

            ImportSessionCommand.NotifyCanExecuteChanged();
        };
    }

    public LocalizationService Localization => LocalizationService.Instance;

    public IReadOnlyList<AppLanguage> Languages { get; } = [AppLanguage.English, AppLanguage.German];

    public ConnectionViewModel Connection { get; }

    public TimelineViewModel Timeline { get; }

    public IClock TimelineClock => _recordingClock;

    public RecordingViewModel Recording { get; }

    public TrafficLogViewModel Log { get; }

    public McpServerViewModel Mcp { get; }

    public ThemeViewModel Theme { get; }

    private void Wire(ICommandStationConnection connection)
    {
        if (!_wired.Add(connection)) return;

        connection.FeedbackReceived += (_, states) => _post(() =>
        {
            if (Recording.ShouldRecordFeedback) _ingest.Apply(states, _recordingClock.Now);
        });
        connection.ConnectionChanged += (_, connected) => _post(() =>
        {
            Connection.IsConnected = connected;
            Log.AppendConnection(connected, Connection.IsSimulated);
            if (Recording.IsRecording)
                _registry.GetOrCreate<ConnectionSource>("connection").Set(connected, _recordingClock.Now);
        });
        connection.TrackPowerChanged += (_, on) => _post(() =>
        {
            Connection.TrackPowerOn = on;
            Log.AppendTrackPower(on);
        });
        connection.HardwareInfoReceived += (_, hardware) => _post(() =>
        {
            _hardware = hardware;
            _stationLimit = _stationCurrentLimits.Lookup(hardware);
        });
        connection.SystemStateReceived += (_, snapshot) => _post(() =>
        {
            Log.AppendSystem(snapshot);
            if (Recording.IsRecording)
            {
                _registry.GetOrCreate<TrackPowerSource>("trackpower").Apply(snapshot, _recordingClock.Now);
                _registry.GetOrCreate<SystemCurrentSource>("systemcurrent")
                    .Apply(snapshot.MainCurrentMilliamps, _hardware.TypeCode, _stationLimit?.Name, _stationLimit?.MaxCurrentMilliamps, _recordingClock.Now);
            }
        });
        connection.LocoInfoReceived += (_, loco) => _post(() =>
        {
            if (!CaptureTrainData) return;
            if (Recording.ShouldRecordFeedback) _locoIngest.Apply(loco, _recordingClock.Now);
            Log.AppendLoco(loco);
        });
        connection.TurnoutInfoReceived += (_, turnout) => _post(() => Log.AppendTurnout(turnout));
    }

    [RelayCommand]
    private async Task SaveSession()
    {
        var path = await _chooseSaveJsonPath();
        if (string.IsNullOrEmpty(path)) return;
        _sessionStore.SaveJson(Timeline.ToSession(Log.Entries.ToList()), path);
    }

    private bool CanImportSession => !Recording.IsRecording;

    [RelayCommand(CanExecute = nameof(CanImportSession))]
    private async Task ImportSession()
    {
        var path = await _chooseOpenJsonPath();
        if (string.IsNullOrEmpty(path)) return;
        var session = _sessionStore.LoadJson(path);
        Timeline.LoadSession(session);
        Log.LoadSession(session.TrafficLog ?? Array.Empty<LogEntry>());
    }

    [RelayCommand]
    private Task OpenSettings() => _openSettings();

    partial void OnCaptureTrainDataChanged(bool value) =>
        _settings.Save(_settings.Load() with { CaptureTrainData = value });

    partial void OnSelectedLanguageChanged(AppLanguage value) =>
        SetLanguage(value == AppLanguage.German ? GermanCode : EnglishCode);

    [RelayCommand]
    private void SetLanguage(string code)
    {
        Localization.Apply(code);
        _settings.Save(_settings.Load() with { Language = code });
    }
}
