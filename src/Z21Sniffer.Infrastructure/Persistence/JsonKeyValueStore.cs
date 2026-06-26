using System.Collections.Concurrent;
using System.Text.Json;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Persistence;

public sealed class JsonKeyValueStore : IKeyValueStore
{
    private readonly Lazy<ConcurrentDictionary<string, object?>> _data;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public JsonKeyValueStore(string filePath)
    {
        _filePath = filePath;
        _data = new Lazy<ConcurrentDictionary<string, object?>>(Load);
    }

    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (!_data.Value.TryGetValue(key, out var value) || value is null) return defaultValue;
        if (value is JsonElement element) return element.Deserialize<T>(_options);
        return (T?)value;
    }

    public void SetValue<T>(string key, T value)
    {
        _data.Value[key] = value;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_data.Value, _options));
    }

    private ConcurrentDictionary<string, object?> Load()
    {
        if (!File.Exists(_filePath)) return new ConcurrentDictionary<string, object?>();
        return JsonSerializer.Deserialize<ConcurrentDictionary<string, object?>>(File.ReadAllText(_filePath), _options)
            ?? new ConcurrentDictionary<string, object?>();
    }
}
