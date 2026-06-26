using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Persistence;

public sealed class FileLogTextStore : ILogTextStore
{
    public void Save(string text, string path) => File.WriteAllText(path, text);
}
