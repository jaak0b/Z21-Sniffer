using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public sealed class IntervalSourceRegistry : IIntervalSourceRegistry
{
    private readonly List<IIntervalSource> _sources = new();
    private readonly IKeyValueStore _store;

    public IntervalSourceRegistry() : this(new InMemoryKeyValueStore())
    {
    }

    public IntervalSourceRegistry(IKeyValueStore store) => _store = store;

    public IReadOnlyList<IIntervalSource> Sources => _sources.OrderBy(source => source.Order).ToList();

    public event EventHandler? Changed;

    public T GetOrCreate<T>(string key, Action<T>? initialize = null) where T : IIntervalSource, new()
    {
        if (_sources.FirstOrDefault(source => source.Id == key) is { } existing) return (T)existing;

        var created = new T { Id = key };
        created.UsePersistence(_store);
        created.SeedOrder(NextOrder());
        initialize?.Invoke(created);
        Attach(created);
        _sources.Add(created);
        RaiseChanged();
        return created;
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

        var seed = 0;
        foreach (var source in sources)
        {
            source.UsePersistence(_store);
            source.SeedOrder(seed++);
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

    private int NextOrder() => _sources.Count == 0 ? 0 : _sources.Max(source => source.Order) + 1;

    private void Attach(IIntervalSource source) => source.Changed += OnSourceChanged;

    private void Detach(IIntervalSource source) => source.Changed -= OnSourceChanged;

    private void OnSourceChanged(object? sender, EventArgs e) => RaiseChanged();

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
