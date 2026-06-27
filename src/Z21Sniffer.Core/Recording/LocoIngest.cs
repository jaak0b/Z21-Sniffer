using System.Globalization;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class LocoIngest
{
    private readonly IIntervalSourceRegistry _registry;

    public LocoIngest(IIntervalSourceRegistry registry) => _registry = registry;

    public void Apply(LocoSnapshot snapshot, DateTimeOffset at)
    {
        var source = _registry.GetOrCreate<LocoIntervalSource>(KeyFor(snapshot.Address), created => created.Address = snapshot.Address);
        source.Apply(snapshot.Speed, snapshot.Forward, snapshot.MaxSpeed, at);
    }

    private string KeyFor(int address) =>
        string.Create(CultureInfo.InvariantCulture, $"loco:{address}");
}
