using Microsoft.Extensions.Logging;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.UI.Desktop;

public sealed class AppLog : IAppLog
{
    private readonly ILogger _logger;

    public AppLog(ILogger logger) => _logger = logger;

    public void Error(Exception exception, string message) => _logger.LogError(exception, "{Message}", message);
}
