using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Ports;

public interface ICommandStationConnection : IFeedbackSource
{
    bool IsConnected { get; }

    event EventHandler<bool>? ConnectionChanged;

    event EventHandler<bool>? TrackPowerChanged;

    event EventHandler<SystemSnapshot>? SystemStateReceived;

    event EventHandler<LocoSnapshot>? LocoInfoReceived;

    event EventHandler<TurnoutSnapshot>? TurnoutInfoReceived;

    event EventHandler<StationHardware>? HardwareInfoReceived;

    Task ConnectAsync(string host, int port);

    Task DisconnectAsync();

    Task<bool> ConfirmSessionAsync(CancellationToken token);

    Task RequestCurrentStateAsync();

    Task SetTrackPowerAsync(bool on);
}
