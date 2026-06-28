using Avalonia.Threading;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.UI.Desktop;

public sealed class AvaloniaSnifferApi : ISnifferApi
{
    private readonly WorkspaceViewModel _vm;
    private readonly IClock _clock;
    private readonly SensorSummaryCalculator _calculator;

    public AvaloniaSnifferApi(WorkspaceViewModel vm, IClock clock, SensorSummaryCalculator calculator)
    {
        _vm = vm;
        _clock = clock;
        _calculator = calculator;
    }

    public Task<ConnectionStatus> GetStatusAsync() =>
        OnUi(() => new ConnectionStatus(
            _vm.Connection.IsConnected,
            _vm.Connection.TrackPowerOn,
            _vm.Connection.Host,
            _vm.Connection.Port,
            _vm.Connection.IsSimulated));

    public Task<IReadOnlyList<SensorInfo>> ListSensorsAsync() =>
        OnUi(() => (IReadOnlyList<SensorInfo>)SensorSources()
            .Select(s => new SensorInfo(s.Sensor.Module, s.Sensor.Contact, s.Label, s.CurrentInterval is not null))
            .ToList());

    public Task<IReadOnlyList<FeedbackSensorInterval>> GetIntervalsAsync(SensorKey? sensor, double? sinceSeconds) =>
        OnUi(() =>
        {
            var now = _clock.Now;
            IEnumerable<FeedbackSensorInterval> query = SensorSources().SelectMany(s => s.Intervals);
            if (sensor is { } key) query = query.Where(i => i.Sensor == key);
            if (sinceSeconds is { } seconds)
            {
                var cutoff = now.AddSeconds(-seconds);
                query = query.Where(i => (i.End ?? now) >= cutoff);
            }

            return (IReadOnlyList<FeedbackSensorInterval>)query.ToList();
        });

    public Task<IReadOnlyList<SensorSummary>> GetSummariesAsync() =>
        OnUi(() => _calculator.Summarize(SensorSources().ToList(), _clock.Now));

    public Task<IReadOnlyList<LogLine>> GetRecentEventsAsync(int max) =>
        OnUi(() => _vm.Log.RecentLines(max));

    public Task ConnectAsync(string host, int port, bool simulated) =>
        OnUiAsync(() =>
        {
            _vm.Connection.Host = host;
            _vm.Connection.Port = port;
            _vm.Connection.Source = simulated ? ConnectionSourceType.Simulation : ConnectionSourceType.Z21;
            return _vm.Connection.ConnectAsync();
        });

    public Task DisconnectAsync() => OnUiAsync(() => _vm.Connection.DisconnectAsync());

    public Task StartRecordingAsync() => OnUi(() =>
    {
        if (!_vm.Recording.IsRecording) _vm.Recording.ToggleCommand.Execute(null);
        return true;
    });

    public Task StopRecordingAsync() => OnUi(() =>
    {
        if (_vm.Recording.IsRecording) _vm.Recording.ToggleCommand.Execute(null);
        return true;
    });

    public Task RenameSensorAsync(int module, int contact, string name) => OnUi(() =>
    {
        var sensor = new SensorKey(module, contact);
        if (SensorSources().FirstOrDefault(s => s.Sensor == sensor) is { } source) source.Label = name;
        return true;
    });

    public Task SetTrackPowerAsync(bool on) => OnUiAsync(() => _vm.Connection.SetTrackPowerAsync(on));

    private IEnumerable<FeedbackSensorSource> SensorSources() => _vm.Timeline.Sources.OfType<FeedbackSensorSource>();

    private Task<T> OnUi<T>(Func<T> read) => Dispatcher.UIThread.InvokeAsync(read).GetTask();

    private Task OnUiAsync(Func<Task> action) => Dispatcher.UIThread.InvokeAsync(action);
}
