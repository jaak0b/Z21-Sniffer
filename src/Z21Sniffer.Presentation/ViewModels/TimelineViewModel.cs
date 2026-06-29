using System.Collections.ObjectModel;
using Autofac.Features.Indexed;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class TimelineViewModel : ObservableObject
{
    private readonly IIntervalSourceRegistry _registry;
    private readonly IIndex<Type, IIntervalChartDrawingStrategy> _chartStrategies;
    private readonly IIndex<Type, IIntervalLegendDrawingStrategy> _legendStrategies;
    private readonly IClock _clock;
    private readonly TimelineViewportCalculator _viewport = new();
    private readonly List<string> _rowOrder = new();
    private bool _reconciling;
    private TimelineWindow _window;
    private DateTimeOffset _startedAt;

    [ObservableProperty]
    private bool _following = true;

    [ObservableProperty]
    private double _highlightThresholdSeconds = 0.5;

    [ObservableProperty]
    private double _highlightMinSeconds;

    [ObservableProperty]
    private double _scrollValueSeconds;

    [ObservableProperty]
    private double _scrollMaxSeconds;

    [ObservableProperty]
    private double _windowSeconds = 60;

    public TimelineViewModel(
        IIntervalSourceRegistry registry,
        IIndex<Type, IIntervalChartDrawingStrategy> chartStrategies,
        IIndex<Type, IIntervalLegendDrawingStrategy> legendStrategies,
        IClock clock)
    {
        _registry = registry;
        _chartStrategies = chartStrategies;
        _legendStrategies = legendStrategies;
        Renderer = new BarChartRenderer(chartStrategies);
        Hover = new TimelineHover(chartStrategies);
        _clock = clock;
        _startedAt = clock.Now;
        _window = new TimelineWindow(clock.Now, TimeSpan.FromSeconds(60));
        ZoomFraction = ZoomFractionFor(_window.Duration);
        _registry.Changed += (_, _) => Reconcile();
    }

    public event EventHandler? Changed;

    public event EventHandler? RowsReordered;

    public event EventHandler? CursorMoved;

    public ObservableCollection<LegendRowViewModel> LegendRows { get; } = new();

    public BarChartRenderer Renderer { get; }

    public TimelineHover Hover { get; }

    public IReadOnlyList<IIntervalSource> Sources => _registry.Sources;

    public DateTimeOffset StartedAt => _startedAt;

    public DateTimeOffset ViewportStart { get; private set; }

    public DateTimeOffset ViewportEnd { get; private set; }

    public double ZoomFraction { get; private set; }

    public double? CursorFraction { get; private set; }

    public DateTimeOffset? CursorTime =>
        CursorFraction is { } fraction ? ViewportStart + (ViewportEnd - ViewportStart) * fraction : null;

    public void SetCursor(double x, double width)
    {
        if (width <= 0) return;
        CursorFraction = Math.Clamp(x / width, 0, 1);
        CursorMoved?.Invoke(this, EventArgs.Empty);
    }

    public void ClearCursor()
    {
        if (CursorFraction is null) return;
        CursorFraction = null;
        CursorMoved?.Invoke(this, EventArgs.Empty);
    }

    public double? HighlightUnderSeconds => HighlightThresholdSeconds > 0 ? HighlightThresholdSeconds : null;

    public double? HighlightOverSeconds => HighlightMinSeconds > 0 ? HighlightMinSeconds : null;

    partial void OnHighlightMinSecondsChanged(double value) =>
        HighlightMinSeconds = Math.Min(value, HighlightThresholdSeconds);

    partial void OnHighlightThresholdSecondsChanged(double value) =>
        HighlightMinSeconds = Math.Min(HighlightMinSeconds, value);

    public RecordingSession ToSession(IReadOnlyList<LogEntry> trafficLog) =>
        new(_startedAt, _registry.Sources.ToList(), trafficLog);

    public void LoadSession(RecordingSession session)
    {
        _startedAt = session.StartedAt;
        _registry.Load(session.Sources);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void MoveRow(int fromIndex, int toIndex)
    {
        LegendRows.Move(fromIndex, toIndex);
        _rowOrder.Clear();
        _rowOrder.AddRange(LegendRows.Select(row => row.Source.Id));
        _registry.Reorder(_rowOrder.ToList());
        RowsReordered?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Tick() => Refresh();

    public void BeginSession()
    {
        _startedAt = _clock.Now;
        Following = true;
        foreach (var source in _registry.Sources) source.IsVisible = true;
        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void PanBySeconds(double seconds)
    {
        Following = false;
        _window = _viewport.Pan(_window, TimeSpan.FromSeconds(seconds), _startedAt, _clock.Now);
        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ZoomByFactor(double factor, double anchorFraction)
    {
        _window = _viewport.Zoom(_window, factor, anchorFraction, _startedAt, _clock.Now);
        Following = _viewport.MaxScrollSeconds(_window, _startedAt, _clock.Now) <= 0;
        Refresh();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetScrollSeconds(double offsetSeconds)
    {
        var now = _clock.Now;
        if (offsetSeconds >= _viewport.MaxScrollSeconds(_window, _startedAt, now) - 0.5)
        {
            Following = true;
        }
        else
        {
            Following = false;
            _window = _viewport.WithStartOffset(_window, offsetSeconds, _startedAt, now);
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

    private void Reconcile()
    {
        if (_reconciling) return;
        _reconciling = true;
        try
        {
            ReconcileCore();
        }
        finally
        {
            _reconciling = false;
        }
    }

    private void ReconcileCore()
    {
        var ordered = _registry.Sources.Where(source => source.IsVisible).ToList();
        var ids = ordered.Select(source => source.Id).ToList();
        if (ids.SequenceEqual(_rowOrder)) return;

        LegendRows.Clear();
        foreach (var source in ordered)
        {
            LegendRows.Add(new LegendRowViewModel(source, _legendStrategies[source.IntervalType].CreateContent(source))
            {
                Height = _chartStrategies[source.IntervalType].LaneHeight(ZoomFraction)
            });
        }

        _rowOrder.Clear();
        _rowOrder.AddRange(ids);
        RowsReordered?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Refresh()
    {
        var now = _clock.Now;
        _window = Following
            ? _viewport.Clamp(_window with { End = now }, _startedAt, now)
            : _viewport.Clamp(_window, _startedAt, now);

        ViewportStart = _window.Start;
        ViewportEnd = _window.End;
        ScrollMaxSeconds = _viewport.MaxScrollSeconds(_window, _startedAt, now);
        ScrollValueSeconds = _viewport.ScrollSeconds(_window, _startedAt, now);
        WindowSeconds = _window.Duration.TotalSeconds;
        ZoomFraction = ZoomFractionFor(_window.Duration);
        UpdateRowHeights();
    }

    private double ZoomFractionFor(TimeSpan window)
    {
        var minSeconds = _viewport.MinDuration.TotalSeconds;
        var maxSeconds = _viewport.MaxDuration.TotalSeconds;
        var min = Math.Log(minSeconds);
        var max = Math.Log(maxSeconds);
        var current = Math.Log(Math.Clamp(window.TotalSeconds, minSeconds, maxSeconds));
        return (max - current) / (max - min);
    }

    private void UpdateRowHeights()
    {
        foreach (var row in LegendRows)
        {
            row.Height = _chartStrategies[row.Source.IntervalType].LaneHeight(ZoomFraction);
        }
    }
}
