using System.Text.Json;
using Z21.Core.Model;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Persistence;

public sealed class JsonStationCurrentLimits : IStationCurrentLimits
{
    private const int DefaultMilliamps = 3200;

    private static readonly IReadOnlyDictionary<int, string> NamesByCode = new Dictionary<int, string>
    {
        [Z21HardwareType.Z21Old] = "Z21Old",
        [Z21HardwareType.Z21New] = "Z21New",
        [Z21HardwareType.Smartrail] = "Smartrail",
        [Z21HardwareType.z21Small] = "z21Small",
        [Z21HardwareType.z21Start] = "z21Start",
        [Z21HardwareType.SingleBooster] = "SingleBooster",
        [Z21HardwareType.DualBooster] = "DualBooster",
        [Z21HardwareType.Z21Xl] = "Z21Xl",
        [Z21HardwareType.XlBooster] = "XlBooster",
    };

    private static readonly IReadOnlyDictionary<string, int> Defaults = new Dictionary<string, int>
    {
        ["Z21Old"] = 3200,
        ["Z21New"] = 3200,
        ["Smartrail"] = 500,
        ["z21Small"] = 1700,
        ["z21Start"] = 1700,
        ["SingleBooster"] = 3000,
        ["DualBooster"] = 3000,
        ["Z21Xl"] = 7000,
        ["XlBooster"] = 7000,
    };

    private readonly string _filePath;
    private readonly Lazy<IReadOnlyDictionary<string, int>> _table;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public JsonStationCurrentLimits(string filePath)
    {
        _filePath = filePath;
        _table = new Lazy<IReadOnlyDictionary<string, int>>(LoadOrSeed);
    }

    public int MaxCurrentMilliamps(StationHardware hardware)
    {
        if (!NamesByCode.TryGetValue(hardware.TypeCode, out var name)) return DefaultMilliamps;
        return _table.Value.TryGetValue(name, out var milliamps) ? milliamps : DefaultMilliamps;
    }

    private IReadOnlyDictionary<string, int> LoadOrSeed()
    {
        if (File.Exists(_filePath)) return ReadFile();

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(Defaults, _options));
        return Defaults;
    }

    private IReadOnlyDictionary<string, int> ReadFile()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(_filePath), _options) ?? Defaults;
        }
        catch (JsonException)
        {
            return Defaults;
        }
    }
}
