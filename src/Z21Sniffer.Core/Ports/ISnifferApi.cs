using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Ports;

public interface ISnifferApi
{
    Task<ConnectionStatus> GetStatusAsync();

    Task<IReadOnlyList<SensorInfo>> ListSensorsAsync();

    Task<IReadOnlyList<FeedbackSensorInterval>> GetIntervalsAsync(SensorKey? sensor, double? sinceSeconds);

    Task<IReadOnlyList<SensorSummary>> GetSummariesAsync();

    Task<IReadOnlyList<LogLine>> GetRecentEventsAsync(int max);

    Task ConnectAsync(string host, int port, bool simulated);

    Task DisconnectAsync();

    Task StartRecordingAsync();

    Task StopRecordingAsync();

    Task RenameSensorAsync(int module, int contact, string name);

    Task SetTrackPowerAsync(bool on);
}
