using System.Text.Json;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Persistence;

public sealed class JsonSessionStore : ISessionStore
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void SaveJson(RecordingSession session, string path)
    {
        EnsureDirectory(path);
        File.WriteAllText(path, JsonSerializer.Serialize(session, _options));
    }

    public RecordingSession LoadJson(string path) =>
        JsonSerializer.Deserialize<RecordingSession>(File.ReadAllText(path), _options)
            ?? throw new InvalidDataException($"Could not read a recording session from '{path}'.");

    private void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }
}
