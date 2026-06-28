using System.Text.Json;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Persistence;

public sealed class JsonStationCurrentLimits : IStationCurrentLimits
{
    private readonly string _filePath;
    private readonly Lazy<IReadOnlyDictionary<int, StationCurrentLimit>> _table;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public JsonStationCurrentLimits(string filePath)
    {
        _filePath = filePath;
        _table = new Lazy<IReadOnlyDictionary<int, StationCurrentLimit>>(Load);
    }

    public StationCurrentLimit? Lookup(StationHardware hardware) =>
        _table.Value.TryGetValue(hardware.TypeCode, out var limit) ? limit : null;

    private IReadOnlyDictionary<int, StationCurrentLimit> Load()
    {
        var table = new Dictionary<int, StationCurrentLimit>();
        if (!File.Exists(_filePath)) return table;

        Entry[]? entries;
        try
        {
            entries = JsonSerializer.Deserialize<Entry[]>(File.ReadAllText(_filePath), _options);
        }
        catch (JsonException)
        {
            return table;
        }

        foreach (var entry in entries ?? Array.Empty<Entry>())
        {
            if (entry is not null) table[entry.HardwareId] = new StationCurrentLimit(entry.Name, entry.MaxCurrentMilliamps);
        }

        return table;
    }

    private sealed record Entry(int HardwareId, string Name, int MaxCurrentMilliamps);
}
