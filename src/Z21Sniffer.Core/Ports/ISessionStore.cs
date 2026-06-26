using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Ports;

public interface ISessionStore
{
    void SaveJson(RecordingSession session, string path);

    RecordingSession LoadJson(string path);
}
