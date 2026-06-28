namespace Z21Sniffer.Core.Recording;

public sealed class ConnectionHealth
{
    private bool _connected;

    public bool Composite => _connected;

    public HealthTransition Update(bool reachable, bool sessionConfirmed)
    {
        var was = _connected;
        _connected = reachable && sessionConfirmed;

        return new HealthTransition(
            NowConnected: _connected,
            RaiseConnected: _connected && !was,
            RaiseDisconnected: was && !_connected);
    }
}

public readonly record struct HealthTransition(
    bool NowConnected,
    bool RaiseConnected,
    bool RaiseDisconnected);
