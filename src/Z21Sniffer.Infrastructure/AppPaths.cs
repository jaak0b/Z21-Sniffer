using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure;

public sealed class AppPaths : IAppPaths
{
    public AppPaths(string dataDirectory) => DataDirectory = dataDirectory;

    public string DataDirectory { get; }

    public string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public string KeyValueFile => Path.Combine(DataDirectory, "kv.json");

    public string LogsDirectory => Path.Combine(DataDirectory, "logs");

    public string SessionsDirectory => Path.Combine(DataDirectory, "sessions");
}
