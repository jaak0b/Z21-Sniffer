using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Infrastructure.Persistence;

public sealed class JsonSessionStore : ISessionStore
{
    private readonly JsonSerializerOptions _options;

    public JsonSessionStore()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { AddSourcePolymorphism, DropTransientMembers },
            },
        };
    }

    public void SaveJson(RecordingSession session, string path)
    {
        EnsureDirectory(path);
        File.WriteAllText(path, JsonSerializer.Serialize(session, _options));
    }

    public RecordingSession LoadJson(string path) =>
        JsonSerializer.Deserialize<RecordingSession>(File.ReadAllText(path), _options)
            ?? throw new InvalidDataException($"Could not read a recording session from '{path}'.");

    private void AddSourcePolymorphism(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(IIntervalSource)) return;

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            DerivedTypes =
            {
                new JsonDerivedType(typeof(FeedbackSensorSource), "sensor"),
                new JsonDerivedType(typeof(ConnectionSource), "connection"),
            },
        };
    }

    private void DropTransientMembers(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties
                     .Where(property => property.Name is "IntervalType" or "CurrentInterval" or "Order" or "Label")
                     .ToList())
        {
            typeInfo.Properties.Remove(property);
        }
    }

    private void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }
}
