namespace Z21Sniffer.Core.Ports;

public interface INetworkReachability
{
    Task<bool> IsReachableAsync(string host, TimeSpan timeout, CancellationToken token = default);
}
