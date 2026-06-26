using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public sealed class FeedbackRecorder
{
    private readonly IClock _clock;
    private readonly Dictionary<SensorKey, bool> _occupied = new();
    private readonly Dictionary<SensorKey, int> _openInterval = new();
    private readonly List<SensorInterval> _intervals = new();
    private DateTimeOffset _startedAt;

    public FeedbackRecorder(IClock clock)
    {
        _clock = clock;
        _startedAt = clock.Now;
    }

    public event EventHandler? Changed;

    public event EventHandler<SensorEdge>? EdgeDetected;

    public DateTimeOffset StartedAt => _startedAt;

    public IReadOnlyList<SensorInterval> Intervals => _intervals;

    public void Apply(IReadOnlyList<SensorState> states)
    {
        var now = _clock.Now;
        var changed = false;
        foreach (var state in states)
        {
            var wasOccupied = _occupied.TryGetValue(state.Sensor, out var occupied) && occupied;
            if (state.Occupied == wasOccupied) continue;

            _occupied[state.Sensor] = state.Occupied;
            if (state.Occupied)
            {
                _openInterval[state.Sensor] = _intervals.Count;
                _intervals.Add(new SensorInterval(state.Sensor, now, End: null));
            }
            else if (_openInterval.Remove(state.Sensor, out var index))
            {
                _intervals[index] = _intervals[index] with { End = now };
            }

            changed = true;
            EdgeDetected?.Invoke(this, new SensorEdge(state.Sensor, state.Occupied, now));
        }

        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _occupied.Clear();
        _openInterval.Clear();
        _intervals.Clear();
        _startedAt = _clock.Now;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(SensorKey sensor)
    {
        _occupied.Remove(sensor);
        _intervals.RemoveAll(interval => interval.Sensor == sensor);

        _openInterval.Clear();
        for (var index = 0; index < _intervals.Count; index++)
        {
            if (_intervals[index].End is null) _openInterval[_intervals[index].Sensor] = index;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Restore(RecordingSession session)
    {
        _occupied.Clear();
        _openInterval.Clear();
        _intervals.Clear();
        _startedAt = session.StartedAt;
        _intervals.AddRange(session.Intervals);

        for (var index = 0; index < _intervals.Count; index++)
        {
            if (_intervals[index].End is null)
            {
                _occupied[_intervals[index].Sensor] = true;
                _openInterval[_intervals[index].Sensor] = index;
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public RecordingSession ToSession() => new(_startedAt, _intervals.ToList());
}
