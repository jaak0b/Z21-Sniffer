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

    Task ConnectAsync(string host, int port);

    Task DisconnectAsync();

    Task RequestCurrentStateAsync();

    Task SetTrackPowerAsync(bool on);
}
