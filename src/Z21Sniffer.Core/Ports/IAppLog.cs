namespace Z21Sniffer.Core.Ports;

public interface IAppLog
{
    void Error(Exception exception, string message);
}
