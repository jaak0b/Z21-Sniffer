namespace Z21Sniffer.Core.Ports;

public interface IAppUpdater
{
    Task<bool> CheckForUpdateAsync();

    Task DownloadUpdateAsync();

    void ApplyUpdateOnExit();
}
