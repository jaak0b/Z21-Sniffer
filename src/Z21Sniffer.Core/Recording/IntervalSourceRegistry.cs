using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public sealed class IntervalSourceRegistry : IIntervalSourceRegistry
{
    private readonly List<IIntervalSource> _sources = new();
    private readonly IKeyValueStore _store;
    private readonly IntervalSourceOrderRegistry _order;

    public IntervalSourceRegistry() : this(new InMemoryKeyValueStore())
    {
    }

    public IntervalSourceRegistry(IKeyValueStore store)
    {
        _store = store;
        _order = new IntervalSourceOrderRegistry(store);
    }

    public IReadOnlyList<IIntervalSource> Sources => _sources.OrderBy(source => _order.IndexOf(source.Id)).ToList();

    public event EventHandler? Changed;

    public T GetOrCreate<T>(string key, Action<T>? initialize = null) where T : IIntervalSource, new()
    {
        if (_sources.FirstOrDefault(source => source.Id == key) is { } existing) return (T)existing;

        var created = new T { Id = key };
        created.UsePersistence(_store);
        var lastOfType = _sources
            .Where(source => source.IntervalType == created.IntervalType)
            .OrderBy(source => _order.IndexOf(source.Id))
            .LastOrDefault();
        _order.Insert(created.Id, lastOfType?.Id);
        initialize?.Invoke(created);
        Attach(created);
        _sources.Add(created);
        RaiseChanged();
        return created;
    }

    public void Reorder(IReadOnlyList<string> orderedIds) => _order.Reorder(orderedIds);

    public void ResetOrder()
    {
        _order.Clear();
        foreach (var source in CanonicalOrder()) _order.Register(source.Id);
        RaiseChanged();
    }

    private IReadOnlyList<IIntervalSource> CanonicalOrder()
    {
        DateTimeOffset FirstStart(IIntervalSource source) =>
            source.Intervals.Count > 0 ? source.Intervals.Min(interval => interval.Start) : DateTimeOffset.MaxValue;

        return _sources
            .GroupBy(source => source.IntervalType)
            .OrderBy(group => group.Min(FirstStart))
            .SelectMany(group => group.OrderBy(FirstStart))
            .ToList();
    }

    public void ResetAliases()
    {
        foreach (var key in _store.Keys().Where(key => key.EndsWith(AliasKeys.Suffix, StringComparison.Ordinal)).ToList())
        {
            _store.Remove(key);
        }

        RaiseChanged();
    }

    public IIntervalSource? Find(string key) => _sources.FirstOrDefault(source => source.Id == key);

    public void Remove(IIntervalSource source)
    {
        if (!_sources.Remove(source)) return;

        Detach(source);
        RaiseChanged();
    }

    public void Load(IEnumerable<IIntervalSource> sources)
    {
        foreach (var existing in _sources) Detach(existing);
        _sources.Clear();

        foreach (var source in sources)
        {
            source.UsePersistence(_store);
            _order.Register(source.Id);
            Attach(source);
            _sources.Add(source);
        }

        RaiseChanged();
    }

    public void Clear()
    {
        foreach (var source in _sources) Detach(source);
        _sources.Clear();
        RaiseChanged();
    }

    private void Attach(IIntervalSource source) => source.Changed += OnSourceChanged;

    private void Detach(IIntervalSource source) => source.Changed -= OnSourceChanged;

    private void OnSourceChanged(object? sender, EventArgs e) => RaiseChanged();

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
