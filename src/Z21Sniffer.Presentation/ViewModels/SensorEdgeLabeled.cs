using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed record SensorEdgeLabeled(string Label, SensorKey Sensor, bool Occupied, DateTimeOffset At);
