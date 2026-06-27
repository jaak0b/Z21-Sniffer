using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Simulation;

public sealed class SimulatedCommandStationConnection : ICommandStationConnection, IDisposable
{
    private readonly SimulatedFeedbackScript _script;
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(200);
    private Timer? _timer;
    private int _tick;

    public SimulatedCommandStationConnection(SimulatedFeedbackScript script) => _script = script;

    public event EventHandler<IReadOnlyList<SensorState>>? FeedbackReceived;

    public event EventHandler<bool>? ConnectionChanged;

    public event EventHandler<bool>? TrackPowerChanged;

    public event EventHandler<SystemSnapshot>? SystemStateReceived;

    public event EventHandler<LocoSnapshot>? LocoInfoReceived;

    public event EventHandler<TurnoutSnapshot>? TurnoutInfoReceived;

    public bool IsConnected { get; private set; }

    public async Task ConnectAsync(string host, int port)
    {
        _timer?.Dispose();
        IsConnected = true;
        ConnectionChanged?.Invoke(this, true);
        TrackPowerChanged?.Invoke(this, true);
        await RequestCurrentStateAsync();
        _timer = new Timer(_ => EmitNext(), state: null, _interval, _interval);
    }

    public Task RequestCurrentStateAsync()
    {
        FeedbackReceived?.Invoke(this, _script.Frame(_tick));
        SystemStateReceived?.Invoke(this, _script.System(_tick));
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        _timer?.Dispose();
        _timer = null;
        ConnectionChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    public Task SetTrackPowerAsync(bool on)
    {
        TrackPowerChanged?.Invoke(this, on);
        return Task.CompletedTask;
    }

    public void EmitNext()
    {
        var tick = Interlocked.Increment(ref _tick);
        FeedbackReceived?.Invoke(this, _script.Frame(tick));
        if (tick % 5 == 0) SystemStateReceived?.Invoke(this, _script.System(tick));
        if (tick % 12 == 0) LocoInfoReceived?.Invoke(this, _script.Loco(tick));
        if (tick % 18 == 0) TurnoutInfoReceived?.Invoke(this, _script.Turnout(tick));
    }

    public void Dispose() => _timer?.Dispose();
}
