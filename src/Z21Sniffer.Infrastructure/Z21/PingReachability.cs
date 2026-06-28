using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Z21;

public sealed class PingReachability : INetworkReachability
{
    private readonly ILogger<PingReachability> _logger;

    public PingReachability(ILogger<PingReachability> logger) => _logger = logger;

    public async Task<bool> IsReachableAsync(string host, TimeSpan timeout, CancellationToken token = default)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, timeout, cancellationToken: token);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception exception) when (exception is PingException or SocketException or ArgumentException)
        {
            _logger.LogDebug(exception, "Ping to {Host} did not succeed", host);
            return false;
        }
    }
}
