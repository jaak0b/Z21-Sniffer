namespace Z21Sniffer.Core.Model;

public sealed record SensorSummary(
    int Module,
    int Contact,
    string Label,
    int OnCount,
    double TotalOnSeconds,
    double? ShortestOnSeconds,
    double? LongestOnSeconds);
