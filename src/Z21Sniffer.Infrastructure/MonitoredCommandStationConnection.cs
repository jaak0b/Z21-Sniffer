using Microsoft.Extensions.Logging;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Infrastructure;

public sealed class MonitoredCommandStationConnection : ICommandStationConnection, IDisposable
{
    private readonly TimeSpan _pingInterval = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(1);

    private readonly ICommandStationConnection _inner;
    private readonly INetworkReachability _reachability;
    private readonly ILogger<MonitoredCommandStationConnection> _logger;
    private readonly ConnectionHealth _health = new();
    private Timer? _timer;
    private string _host = string.Empty;

    private readonly int _refreshEveryPolls = 4;

    private CancellationTokenSource? _confirmCts;
    private bool _reachable;
    private bool _confirming;
    private bool _sessionConfirmed;
    private int _pollsSinceRefresh;

    public MonitoredCommandStationConnection(
        ICommandStationConnection inner,
        INetworkReachability reachability,
        ILogger<MonitoredCommandStationConnection> logger)
    {
        _inner = inner;
        _reachability = reachability;
        _logger = logger;
        _inner.FeedbackReceived += (_, states) => FeedbackReceived?.Invoke(this, states);
        _inner.TrackPowerChanged += (_, on) => TrackPowerChanged?.Invoke(this, on);
        _inner.SystemStateReceived += (_, snapshot) => SystemStateReceived?.Invoke(this, snapshot);
        _inner.LocoInfoReceived += (_, loco) => LocoInfoReceived?.Invoke(this, loco);
        _inner.TurnoutInfoReceived += (_, turnout) => TurnoutInfoReceived?.Invoke(this, turnout);
    }

    public event EventHandler<IReadOnlyList<SensorState>>? FeedbackReceived;

    public event EventHandler<bool>? ConnectionChanged;

    public event EventHandler<bool>? TrackPowerChanged;

    public event EventHandler<SystemSnapshot>? SystemStateReceived;

    public event EventHandler<LocoSnapshot>? LocoInfoReceived;

    public event EventHandler<TurnoutSnapshot>? TurnoutInfoReceived;

    public bool IsConnected => _health.Composite;

    public async Task ConnectAsync(string host, int port)
    {
        _host = host;
        await _inner.ConnectAsync(host, port);
        _timer = new Timer(_ => _ = PollAsync(), state: null, _pingInterval, _pingInterval);
    }

    public async Task PollAsync()
    {
        _reachable = await _reachability.IsReachableAsync(_host, _pingTimeout);

        if (!_reachable)
        {
            _confirmCts?.Cancel();
            _confirming = false;
            _sessionConfirmed = false;
        }
        else if (!_sessionConfirmed && !_confirming)
        {
            StartConfirmation();
        }

        Evaluate();
        await RefreshCurrentStateIfDueAsync();
    }

    private async Task RefreshCurrentStateIfDueAsync()
    {
        if (!_health.Composite)
        {
            _pollsSinceRefresh = 0;
            return;
        }

        if (++_pollsSinceRefresh < _refreshEveryPolls) return;

        _pollsSinceRefresh = 0;
        await _inner.RequestCurrentStateAsync();
    }

    public async Task DisconnectAsync()
    {
        _timer?.Dispose();
        _timer = null;
        _confirmCts?.Cancel();
        await _inner.DisconnectAsync();
        _reachable = false;
        _sessionConfirmed = false;
        _confirming = false;
        Evaluate();
    }

    public Task<bool> ConfirmSessionAsync(CancellationToken token) => _inner.ConfirmSessionAsync(token);

    public Task RequestCurrentStateAsync() => _inner.RequestCurrentStateAsync();

    public Task SetTrackPowerAsync(bool on) => _inner.SetTrackPowerAsync(on);

    public void Dispose()
    {
        _timer?.Dispose();
        _confirmCts?.Cancel();
    }

    private void StartConfirmation()
    {
        _confirming = true;
        _confirmCts = new CancellationTokenSource();
        _ = ConfirmAsync(_confirmCts.Token);
    }

    private async Task ConfirmAsync(CancellationToken token)
    {
        bool confirmed;
        try
        {
            confirmed = await _inner.ConfirmSessionAsync(token);
        }
        catch (OperationCanceledException)
        {
            confirmed = false;
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Session confirmation attempt failed; will retry on the next poll");
            confirmed = false;
        }

        _confirming = false;
        if (confirmed && !token.IsCancellationRequested)
        {
            _sessionConfirmed = true;
            Evaluate();
        }
    }

    private void Evaluate()
    {
        var transition = _health.Update(_reachable, _sessionConfirmed);
        if (transition.RaiseConnected) ConnectionChanged?.Invoke(this, true);
        if (transition.RaiseDisconnected) ConnectionChanged?.Invoke(this, false);
    }
}
