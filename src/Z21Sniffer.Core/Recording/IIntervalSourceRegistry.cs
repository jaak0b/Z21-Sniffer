namespace Z21Sniffer.Core.Recording;

public interface IIntervalSourceRegistry
{
    IReadOnlyList<IIntervalSource> Sources { get; }

    T GetOrCreate<T>(string key, Action<T>? initialize = null) where T : IIntervalSource, new();

    IIntervalSource? Find(string key);

    void Remove(IIntervalSource source);

    void Load(IEnumerable<IIntervalSource> sources);

    void Reorder(IReadOnlyList<string> orderedIds);

    void Clear();

    event EventHandler? Changed;
}
