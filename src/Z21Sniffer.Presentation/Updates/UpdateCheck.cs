using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Presentation.Updates;

public sealed class UpdateCheck
{
    private readonly IAppUpdater _updater;
    private readonly IAppLog _log;

    public UpdateCheck(IAppUpdater updater, IAppLog log)
    {
        _updater = updater;
        _log = log;
    }

    public async Task RunAsync()
    {
        try
        {
            if (!await _updater.CheckForUpdateAsync()) return;

            await _updater.DownloadUpdateAsync();
            _updater.ApplyUpdateOnExit();
        }
        catch (Exception exception)
        {
            _log.Error(exception, "Background update check failed.");
        }
    }
}
