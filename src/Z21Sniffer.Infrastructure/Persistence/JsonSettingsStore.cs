using System.Text.Json;
using CommandStation.Transport.Udp;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Persistence;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly string _filePath;

    public JsonSettingsStore(string filePath) => _filePath = filePath;

    public AppSettings Load()
    {
        if (!File.Exists(_filePath)) return Defaults();
        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath), _options) ?? Defaults();
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, _options));
    }

    private AppSettings Defaults() =>
        new(UdpTransportOptions.DefaultAddress, UdpTransportOptions.DefaultPort, "en");
}
