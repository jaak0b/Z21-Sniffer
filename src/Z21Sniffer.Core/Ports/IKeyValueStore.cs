namespace Z21Sniffer.Core.Ports;

public interface IKeyValueStore
{
    T? GetValue<T>(string key, T? defaultValue = default);

    void SetValue<T>(string key, T value);

    void Remove(string key);

    IReadOnlyCollection<string> Keys();
}
