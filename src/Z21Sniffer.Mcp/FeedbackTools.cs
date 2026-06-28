using System.ComponentModel;
using ModelContextProtocol.Server;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Mcp;

[McpServerToolType]
public sealed class FeedbackTools
{
    private readonly ISnifferApi _api;

    public FeedbackTools(ISnifferApi api) => _api = api;

    [McpServerTool(Name = "get_status")]
    [Description("Connection status: whether the Z21 is connected, track power, host/port, and whether the built-in simulator is in use.")]
    public Task<ConnectionStatus> GetStatus() => _api.GetStatusAsync();

    [McpServerTool(Name = "list_sensors")]
    [Description("Every feedback contact seen so far, with its friendly label and whether it is currently reporting occupied.")]
    public Task<IReadOnlyList<SensorInfo>> ListSensors() => _api.ListSensorsAsync();

    [McpServerTool(Name = "get_intervals")]
    [Description("Recorded on-periods. Optionally filter to one sensor (module + contact) and/or to the last N seconds.")]
    public Task<IReadOnlyList<FeedbackSensorInterval>> GetIntervals(int? module, int? contact, double? sinceSeconds)
    {
        var sensor = module is { } m && contact is { } c ? new SensorKey(m, c) : (SensorKey?)null;
        return _api.GetIntervalsAsync(sensor, sinceSeconds);
    }

    [McpServerTool(Name = "get_summaries")]
    [Description("Per-sensor statistics: on-count and total/shortest/longest on-time. A high count with a tiny shortest-on-time points at a flickering (ghost) sensor.")]
    public Task<IReadOnlyList<SensorSummary>> GetSummaries() => _api.GetSummariesAsync();

    [McpServerTool(Name = "get_recent_events")]
    [Description("The most recent raw command-station events (connection, track power, feedback, system).")]
    public Task<IReadOnlyList<LogLine>> GetRecentEvents(int max = 50) => _api.GetRecentEventsAsync(max);

    [McpServerTool(Name = "connect")]
    [Description("Connect to the Z21 at the given host/port, or to the built-in simulator when simulated is true.")]
    public async Task<string> Connect(string host, int port, bool simulated)
    {
        await _api.ConnectAsync(host, port, simulated);
        return simulated ? "Connecting to the simulator." : $"Connecting to {host}:{port}.";
    }

    [McpServerTool(Name = "disconnect")]
    [Description("Disconnect from the command station.")]
    public async Task<string> Disconnect()
    {
        await _api.DisconnectAsync();
        return "Disconnecting.";
    }

    [McpServerTool(Name = "start_recording")]
    [Description("Start a fresh recording: clears the timeline and begins capturing feedback from now.")]
    public async Task<string> StartRecording()
    {
        await _api.StartRecordingAsync();
        return "Recording started.";
    }

    [McpServerTool(Name = "stop_recording")]
    [Description("Stop the current recording. The captured timeline stays on screen for review.")]
    public async Task<string> StopRecording()
    {
        await _api.StopRecordingAsync();
        return "Recording stopped.";
    }

    [McpServerTool(Name = "rename_sensor")]
    [Description("Give a feedback contact (module + contact) a friendly name.")]
    public async Task<string> RenameSensor(int module, int contact, string name)
    {
        await _api.RenameSensorAsync(module, contact, name);
        return $"Renamed M{module}.{contact} to '{name}'.";
    }

    [McpServerTool(Name = "set_track_power")]
    [Description("Switch track power on or off.")]
    public async Task<string> SetTrackPower(bool on)
    {
        await _api.SetTrackPowerAsync(on);
        return on ? "Track power on." : "Track power off.";
    }
}
