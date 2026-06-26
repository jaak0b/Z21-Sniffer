using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class ConnectionViewModel : ObservableObject
{
    private readonly ICommandStationConnectionFactory _factory;
    private readonly ISettingsStore _settings;
    private ICommandStationConnection? _active;

    [ObservableProperty]
    private string _host;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSimulated))]
    [NotifyPropertyChangedFor(nameof(ShowZ21Settings))]
    private ConnectionSourceType _source = ConnectionSourceType.Z21;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleLabel))]
    [NotifyPropertyChangedFor(nameof(CanEditConnection))]
    private bool _isConnected;

    [ObservableProperty]
    private bool _trackPowerOn;

    public ConnectionViewModel(ICommandStationConnectionFactory factory, ISettingsStore settings)
    {
        _factory = factory;
        _settings = settings;
        var loaded = settings.Load();
        _host = loaded.Host;
        _port = loaded.Port;
        WeakReferenceMessenger.Default.Register<ConnectionViewModel, LanguageChangedMessage>(
            this, (recipient, _) => recipient.OnPropertyChanged(nameof(ToggleLabel)));
    }

    public event Action<ICommandStationConnection>? ConnectionActivated;

    public bool IsSimulated => Source == ConnectionSourceType.Simulation;

    public bool ShowZ21Settings => Source == ConnectionSourceType.Z21;

    public string ToggleLabel => LocalizationService.Instance[IsConnected ? "Disconnect" : "Connect"];

    public bool CanEditConnection => !IsConnected;

    [RelayCommand]
    private async Task ToggleConnectionAsync()
    {
        if (IsConnected) await DisconnectAsync();
        else await ConnectAsync();
    }

    public async Task ConnectAsync()
    {
        var connection = _factory.Create(IsSimulated);
        _active = connection;
        ConnectionActivated?.Invoke(connection);
        await connection.ConnectAsync(Host, Port);
        _settings.Save(_settings.Load() with { Host = Host, Port = Port });
    }

    public async Task DisconnectAsync()
    {
        if (_active is not null) await _active.DisconnectAsync();
    }

    public Task SetTrackPowerAsync(bool on) =>
        _active is { } connection
            ? connection.SetTrackPowerAsync(on)
            : throw new InvalidOperationException("Not connected to a command station.");
}
