namespace Z21Sniffer.Core.Ports;

public interface IAppPaths
{
    string DataDirectory { get; }

    string SettingsFile { get; }

    string KeyValueFile { get; }

    string LogsDirectory { get; }

    string SessionsDirectory { get; }

    string HardwareCurrentFile { get; }
}
