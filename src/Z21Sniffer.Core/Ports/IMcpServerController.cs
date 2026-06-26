namespace Z21Sniffer.Core.Ports;

public interface IMcpServerController
{
    bool IsRunning { get; }

    string? Url { get; }

    Task StartAsync(int port);

    Task StopAsync();
}
