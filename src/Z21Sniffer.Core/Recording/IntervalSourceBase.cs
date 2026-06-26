using System.Globalization;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public abstract class IntervalSourceBase<T> : IIntervalSource<T> where T : class, IInterval, new()
{
    private List<T> _intervals = new();
    private int _nextKey;
    private int _orderSeed;

    public string Id { get; set; } = string.Empty;

    public int Order
    {
        get => Persistence.GetValue($"{Id}/order", _orderSeed);
        set => Persistence.SetValue($"{Id}/order", value);
    }

    public Type IntervalType => typeof(T);

    protected IKeyValueStore Persistence { get; private set; } = new InMemoryKeyValueStore();

    public void UsePersistence(IKeyValueStore store) => Persistence = store;

    public void SeedOrder(int order) => _orderSeed = order;

    public T? CurrentInterval { get; private set; }

    public IReadOnlyList<T> Intervals
    {
        get => _intervals;
        set => _intervals = value as List<T> ?? value.ToList();
    }

    IReadOnlyList<IInterval> IIntervalSource.Intervals => _intervals;

    public event EventHandler? Changed;

    public void Upsert(T interval)
    {
        var index = _intervals.FindIndex(existing => existing.Key == interval.Key);
        if (index >= 0) _intervals[index] = interval;
        else _intervals.Add(interval);

        RaiseChanged();
    }

    public void CloseInterval(DateTimeOffset at, IntervalEndReason reason)
    {
        if (CurrentInterval is null) return;

        CurrentInterval.End = at;
        CurrentInterval.EndReason = reason;
        CurrentInterval = null;
        RaiseChanged();
    }

    public void CloseOpenIntervals(DateTimeOffset at, IntervalEndReason reason)
    {
        var closedAny = false;
        foreach (var interval in _intervals)
        {
            if (!interval.IsOpen) continue;
            interval.End = at;
            interval.EndReason = reason;
            closedAny = true;
        }

        CurrentInterval = null;
        if (closedAny) RaiseChanged();
    }

    public void Clear()
    {
        _intervals.Clear();
        CurrentInterval = null;
        RaiseChanged();
    }

    protected T CreateInterval(DateTimeOffset start)
    {
        var interval = new T
        {
            Key = _nextKey.ToString(CultureInfo.InvariantCulture),
            Start = start,
            EndReason = IntervalEndReason.Open,
        };
        _nextKey++;
        _intervals.Add(interval);
        CurrentInterval = interval;
        RaiseChanged();
        return interval;
    }

    protected void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
