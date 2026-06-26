namespace Z21Sniffer.Core.Model;

public sealed record ConnectionStatus(
    bool Connected,
    bool TrackPowerOn,
    string Host,
    int Port,
    bool Simulated);
