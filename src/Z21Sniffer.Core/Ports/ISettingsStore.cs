using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Ports;

public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
