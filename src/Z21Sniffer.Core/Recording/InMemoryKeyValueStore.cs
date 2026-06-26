using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public sealed class InMemoryKeyValueStore : IKeyValueStore
{
    private readonly Dictionary<string, object?> _data = new();

    public T? GetValue<T>(string key, T? defaultValue = default) =>
        _data.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;

    public void SetValue<T>(string key, T value) => _data[key] = value;
}
