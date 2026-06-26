namespace Z21Sniffer.Core.Ports;

public interface ILogTextStore
{
    void Save(string text, string path);
}
