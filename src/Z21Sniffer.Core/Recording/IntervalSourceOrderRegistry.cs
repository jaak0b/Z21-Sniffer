using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public sealed class IntervalSourceOrderRegistry : IIntervalSourceOrderRegistry
{
    private const string Key = "source-order";
    private readonly IKeyValueStore _store;
    private readonly List<string> _ids;

    public IntervalSourceOrderRegistry(IKeyValueStore store)
    {
        _store = store;
        _ids = _store.GetValue<List<string>>(Key) ?? new List<string>();
    }

    public int IndexOf(string id) => _ids.IndexOf(id);

    public void Register(string id) => Insert(id, afterId: null);

    public void Insert(string id, string? afterId)
    {
        if (_ids.Contains(id)) return;

        var anchor = afterId is null ? -1 : _ids.IndexOf(afterId);
        if (anchor < 0) _ids.Add(id);
        else _ids.Insert(anchor + 1, id);
        Save();
    }

    public void Reorder(IReadOnlyList<string> orderedIds)
    {
        var subset = new HashSet<string>(orderedIds);
        var queue = new Queue<string>(orderedIds);
        var merged = new List<string>(_ids.Count);

        foreach (var id in _ids)
        {
            merged.Add(subset.Contains(id) ? queue.Dequeue() : id);
        }

        while (queue.Count > 0) merged.Add(queue.Dequeue());

        _ids.Clear();
        _ids.AddRange(merged);
        Save();
    }

    public void Clear()
    {
        _ids.Clear();
        Save();
    }

    private void Save() => _store.SetValue(Key, new List<string>(_ids));
}
