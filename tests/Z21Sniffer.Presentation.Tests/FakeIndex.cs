using Autofac.Features.Indexed;

namespace Z21Sniffer.Presentation.Tests;

internal sealed class FakeIndex<TKey, TValue> : IIndex<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _map;

    public FakeIndex(Dictionary<TKey, TValue> map) => _map = map;

    public TValue this[TKey key] => _map[key];

    public bool TryGetValue(TKey key, out TValue value) => _map.TryGetValue(key, out value!);
}
