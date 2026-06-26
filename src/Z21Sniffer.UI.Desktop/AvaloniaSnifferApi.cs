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
        OnUi(() =>
        {
            var occupied = _vm.Timeline.Intervals.Where(i => i.IsOpen).Select(i => i.Sensor).ToHashSet();
            return (IReadOnlyList<SensorInfo>)_vm.Timeline.Rows
                .Select(r => new SensorInfo(r.Sensor.Module, r.Sensor.Contact, r.Label, occupied.Contains(r.Sensor)))
                .ToList();
        });

    public Task<IReadOnlyList<SensorInterval>> GetIntervalsAsync(SensorKey? sensor, double? sinceSeconds) =>
        OnUi(() =>
        {
            var now = _clock.Now;
            IEnumerable<SensorInterval> query = _vm.Timeline.Intervals;
            if (sensor is { } key) query = query.Where(i => i.Sensor == key);
            if (sinceSeconds is { } seconds)
            {
                var cutoff = now.AddSeconds(-seconds);
                query = query.Where(i => (i.End ?? now) >= cutoff);
            }

            return (IReadOnlyList<SensorInterval>)query.ToList();
        });

    public Task<IReadOnlyList<SensorSummary>> GetSummariesAsync() =>
        OnUi(() => _calculator.Summarize(_vm.Timeline.Intervals.ToList(), _vm.Timeline.Aliases, _clock.Now));

    public Task<IReadOnlyList<LogLine>> GetRecentEventsAsync(int max) =>
        OnUi(() => _vm.Log.RecentLines(max));

    public Task ConnectAsync(string host, int port, bool simulated) =>
        OnUiAsync(() =>
        {
            _vm.Connection.Host = host;
            _vm.Connection.Port = port;
            _vm.Connection.Source = simulated ? ConnectionSource.Simulation : ConnectionSource.Z21;
            return _vm.Connection.ConnectAsync();
        });

    public Task DisconnectAsync() => OnUiAsync(() => _vm.Connection.DisconnectAsync());

    public Task ClearAsync() => OnUi(() =>
    {
        _vm.Timeline.ClearCommand.Execute(null);
        return true;
    });

    public Task RenameSensorAsync(int module, int contact, string name) => OnUi(() =>
    {
        _vm.Timeline.Rename(new SensorKey(module, contact), name);
        return true;
    });

    public Task SetTrackPowerAsync(bool on) => OnUiAsync(() => _vm.Connection.SetTrackPowerAsync(on));

    private Task<T> OnUi<T>(Func<T> read) => Dispatcher.UIThread.InvokeAsync(read).GetTask();

    private Task OnUiAsync(Func<Task> action) => Dispatcher.UIThread.InvokeAsync(action);
}
