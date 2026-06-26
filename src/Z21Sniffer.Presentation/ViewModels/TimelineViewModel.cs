using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class TimelineViewModel : ObservableObject
{
    private readonly FeedbackRecorder _recorder;
    private readonly SensorLabeler _labeler;
    private readonly IClock _clock;
    private readonly List<SensorAlias> _aliases;
    private readonly List<SensorKey> _savedOrder;
    private readonly HashSet<SensorKey> _knownSensors = new();
    private readonly TimelineViewportCalculator _viewport = new();
    private TimelineWindow _window;

    [ObservableProperty]
    private bool _following = true;

    [ObservableProperty]
    private double _highlightThresholdSeconds = 0.5;

    [ObservableProperty]
    private double _scrollValueSeconds;

    [ObservableProperty]
    private double _scrollMaxSeconds;

    [ObservableProperty]
    private double _windowSeconds = 60;

    public TimelineViewModel(
        FeedbackRecorder recorder,
        SensorLabeler labeler,
        IClock clock,
        IReadOnlyList<SensorAlias> aliases,
        IReadOnlyList<SensorKey> savedOrder)
    {
        _recorder = recorder;
        _labeler = labeler;
        _clock = clock;
        _aliases = aliases.ToList();
        _savedOrder = savedOrder.ToList();
        _window = new TimelineWindow(clock.Now, TimeSpan.FromSeconds(60));
        _recorder.EdgeDetected += OnEdgeDetected;
    }

    public event EventHandler? Changed;

    public event EventHandler<SensorEdgeLabeled>? SensorEdgeDetected;

    public event EventHandler? AliasesChanged;

    public event EventHandler? RowsReordered;

    public ObservableCollection<SensorRowViewModel> Rows { get; } = new();

    public IReadOnlyList<SensorInterval> Intervals => _recorder.Intervals;

    public IReadOnlyList<SensorAlias> Aliases => _aliases;

    public IReadOnlyList<SensorKey> Order => Rows.Select(r => r.Sensor).ToList();

    public DateTimeOffset ViewportStart { get; private set; }

    public DateTimeOffset ViewportEnd { get; private set; }

    public double? HighlightUnderSeconds => HighlightThresholdSeconds > 0 ? HighlightThresholdSeconds : null;

    public RecordingSession ToSession() => _recorder.ToSession();

    public void LoadSession(RecordingSession session)
    {
        _recorder.Restore(session);
        _knownSensors.Clear();
        Rows.Clear();

        foreach (var interval in session.Intervals)
        {
            if (_knownSensors.Add(interval.Sensor)) InsertRowOrdered(interval.Sensor);
        }

        RowsReordered?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void OnFeedback(IReadOnlyList<SensorState> states)
    {
        _recorder.Apply(states);
        foreach (var state in states)
        {
            if (state.Occupied && _knownSensors.Add(state.Sensor))
            {
                InsertRowOrdered(state.Sensor);
                RowsReordered?.Invoke(this, EventArgs.Empty);
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Rename(SensorKey sensor, string name)
    {
        _aliases.RemoveAll(a => a.Sensor == sensor);
        _aliases.Add(new SensorAlias(sensor, name));

        var row = Rows.FirstOrDefault(r => r.Sensor == sensor);
        if (row is not null) row.Label = _labeler.Label(sensor, _aliases);

        AliasesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveSensor(SensorKey sensor)
    {
        _recorder.Remove(sensor);
        _knownSensors.Remove(sensor);
        _aliases.RemoveAll(a => a.Sensor == sensor);
        var row = Rows.FirstOrDefault(r => r.Sensor == sensor);
        if (row is not null) Rows.Remove(row);

        RowsReordered?.Invoke(this, EventArgs.Empty);
        AliasesChanged?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void MoveRow(int fromIndex, int toIndex)
    {
        Rows.Move(fromIndex, toIndex);
        RowsReordered?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Tick() => Refresh();

    public void PanBySeconds(double seconds)
    {
        Following = false;
        _window = _viewport.Pan(_window, TimeSpan.FromSeconds(seconds), _recorder.StartedAt, _clock.Now);
        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ZoomByFactor(double factor, double anchorFraction)
    {
        Following = false;
        _window = _viewport.Zoom(_window, factor, anchorFraction, _recorder.StartedAt, _clock.Now);
        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetScrollSeconds(double offsetSeconds)
    {
        var now = _clock.Now;
        var earliest = _recorder.StartedAt;
        if (offsetSeconds >= _viewport.MaxScrollSeconds(_window, earliest, now) - 0.5)
        {
            Following = true;
        }
        else
        {
            Following = false;
            _window = _viewport.WithStartOffset(_window, offsetSeconds, earliest, now);
        }

        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void JumpToLive()
    {
        Following = true;
        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void TogglePause()
    {
        Following = !Following;
        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Clear()
    {
        _recorder.Clear();
        _knownSensors.Clear();
        Rows.Clear();
        RowsReordered?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CommitRename(SensorRowViewModel row) => Rename(row.Sensor, row.Label);

    private void Refresh()
    {
        var now = _clock.Now;
        var earliest = _recorder.StartedAt;
        _window = Following
            ? _viewport.Clamp(_window with { End = now }, earliest, now)
            : _viewport.Clamp(_window, earliest, now);

        ViewportStart = _window.Start;
        ViewportEnd = _window.End;
        ScrollMaxSeconds = _viewport.MaxScrollSeconds(_window, earliest, now);
        ScrollValueSeconds = _viewport.ScrollSeconds(_window, earliest, now);
        WindowSeconds = _window.Duration.TotalSeconds;
    }

    private void OnEdgeDetected(object? sender, SensorEdge edge) =>
        SensorEdgeDetected?.Invoke(this, new SensorEdgeLabeled(
            _labeler.Label(edge.Sensor, _aliases), edge.Sensor, edge.Occupied, edge.At));

    private void InsertRowOrdered(SensorKey sensor)
    {
        var row = new SensorRowViewModel(sensor, _labeler.Label(sensor, _aliases));
        var desired = _savedOrder.IndexOf(sensor);
        if (desired < 0)
        {
            Rows.Add(row);
            return;
        }

        var insertAt = Rows.Count;
        for (var i = 0; i < Rows.Count; i++)
        {
            var existing = _savedOrder.IndexOf(Rows[i].Sensor);
            if (existing < 0 || existing > desired)
            {
                insertAt = i;
                break;
            }
        }

        Rows.Insert(insertAt, row);
    }
}
