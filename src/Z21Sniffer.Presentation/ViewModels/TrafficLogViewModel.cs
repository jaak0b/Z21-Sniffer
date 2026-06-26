using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Logging;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class TrafficLogViewModel : ObservableObject
{
    private readonly LocalizationService _localization;
    private readonly IClock _clock;
    private readonly int _maxEntries;
    private readonly List<LogEntry> _all = new();
    private readonly LogFilter _filter = new();
    private readonly LogTextFormatter _formatter = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    public TrafficLogViewModel(LocalizationService localization, IClock clock, int maxEntries = 500)
    {
        _localization = localization;
        _clock = clock;
        _maxEntries = maxEntries;

        foreach (var kind in Enum.GetValues<LogEntryKind>())
        {
            var toggle = new KindToggle(kind, localization);
            toggle.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(KindToggle.IsSelected)) Refilter();
            };
            KindToggles.Add(toggle);
        }
    }

    public event EventHandler? EntryAppended;

    public ObservableCollection<LogEntry> Filtered { get; } = new();

    public ObservableCollection<KindToggle> KindToggles { get; } = new();

    public void AppendConnection(bool connected, bool simulated)
    {
        var message = _localization[connected ? "LogConnected" : "LogDisconnected"];
        if (simulated) message += _localization["LogSimulatedSuffix"];
        Append(new LogEntry(_clock.Now, LogEntryKind.Connection, message));
    }

    public void AppendTrackPower(bool on) =>
        Append(new LogEntry(_clock.Now, LogEntryKind.TrackPower, _localization[on ? "LogTrackPowerOn" : "LogTrackPowerOff"]));

    public void AppendSystem(SystemSnapshot snapshot)
    {
        var volts = (snapshot.SupplyVoltageMillivolts / 1000.0).ToString("0.0", CultureInfo.CurrentCulture);
        var message = string.Format(
            CultureInfo.CurrentCulture,
            _localization["LogSystemTelemetry"],
            snapshot.MainCurrentMilliamps, volts, snapshot.TemperatureCelsius);

        var faults = Faults(snapshot);
        if (faults.Count > 0) message += " · " + string.Join(" · ", faults);

        Append(new LogEntry(_clock.Now, LogEntryKind.System, message, faults.Count > 0));
    }

    public void AppendLoco(LocoSnapshot loco)
    {
        var direction = _localization[loco.Forward ? "LogForward" : "LogBackward"];
        var message = string.Format(CultureInfo.CurrentCulture, _localization["LogLoco"], loco.Address, direction, loco.Speed);
        Append(new LogEntry(_clock.Now, LogEntryKind.Loco, message));
    }

    public void AppendTurnout(TurnoutSnapshot turnout)
    {
        var position = _localization[turnout.Position switch
        {
            TurnoutPosition.Output1 => "LogTurnoutOutput1",
            TurnoutPosition.Output2 => "LogTurnoutOutput2",
            _ => "LogTurnoutUnknown"
        }];
        var message = string.Format(CultureInfo.CurrentCulture, _localization["LogTurnout"], turnout.Address, position);
        Append(new LogEntry(_clock.Now, LogEntryKind.Turnout, message));
    }

    public void AppendSensorEdge(string label, SensorKey sensor, bool occupied, DateTimeOffset at)
    {
        var address = $"M{sensor.Module}.{sensor.Contact}";
        var name = label == address ? address : $"{label} ({address})";
        var template = _localization[occupied ? "LogSensorOccupied" : "LogSensorClear"];
        Append(new LogEntry(at, LogEntryKind.Sensor, string.Format(CultureInfo.CurrentCulture, template, name)));
    }

    public IReadOnlyList<LogLine> RecentLines(int max) =>
        _all.TakeLast(max).Select(e => new LogLine(e.Timestamp, e.Kind.ToString(), e.Message)).ToList();

    public string BuildExportText() => _formatter.Format(Filtered);

    [RelayCommand]
    private void Clear()
    {
        _all.Clear();
        Filtered.Clear();
    }

    partial void OnSearchTextChanged(string value) => Refilter();

    private void Append(LogEntry entry)
    {
        _all.Add(entry);
        if (_all.Count > _maxEntries)
        {
            var dropped = _all[0];
            _all.RemoveAt(0);
            Filtered.Remove(dropped);
        }

        if (!_filter.Matches(entry, EnabledKinds(), SearchText)) return;
        Filtered.Add(entry);
        EntryAppended?.Invoke(this, EventArgs.Empty);
    }

    private void Refilter()
    {
        var enabled = EnabledKinds();
        Filtered.Clear();
        foreach (var entry in _all)
        {
            if (_filter.Matches(entry, enabled, SearchText)) Filtered.Add(entry);
        }
    }

    private IReadOnlySet<LogEntryKind> EnabledKinds() =>
        KindToggles.Where(t => t.IsSelected).Select(t => t.Kind).ToHashSet();

    private List<string> Faults(SystemSnapshot snapshot)
    {
        var faults = new List<string>();
        if (snapshot.ShortCircuit) faults.Add(_localization["LogFaultShortCircuit"]);
        if (snapshot.EmergencyStop) faults.Add(_localization["LogFaultEmergencyStop"]);
        if (snapshot.TrackVoltageOff) faults.Add(_localization["LogFaultTrackVoltageOff"]);
        if (snapshot.PowerLost) faults.Add(_localization["LogFaultPowerLost"]);
        if (snapshot.HighTemperature) faults.Add(_localization["LogFaultHighTemperature"]);
        return faults;
    }
}
