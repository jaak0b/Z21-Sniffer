namespace Z21Sniffer.Core.Model;

public sealed record AppSettings(
    string Host,
    int Port,
    string Language,
    int McpPort = 8731,
    bool DarkTheme = true,
    bool CaptureTrainData = false);
