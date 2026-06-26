namespace Z21Sniffer.Core.Model;

public sealed record SensorEdge(SensorKey Sensor, bool Occupied, DateTimeOffset At);
