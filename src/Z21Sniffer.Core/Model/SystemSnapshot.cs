namespace Z21Sniffer.Core.Model;

public sealed record SystemSnapshot(
    int MainCurrentMilliamps,
    int SupplyVoltageMillivolts,
    int TemperatureCelsius,
    bool ShortCircuit,
    bool EmergencyStop,
    bool TrackVoltageOff,
    bool PowerLost,
    bool HighTemperature);
