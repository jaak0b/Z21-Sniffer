namespace Z21Sniffer.Core.Model;

public sealed record AppSettings(
    string Host,
    int Port,
    string Language,
    IReadOnlyList<SensorAlias> Aliases,
    int McpPort = 8731,
    bool DarkTheme = false,
    IReadOnlyList<SensorKey>? SensorOrder = null);
