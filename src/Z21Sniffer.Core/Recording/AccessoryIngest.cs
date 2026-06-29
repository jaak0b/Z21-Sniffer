using System.Globalization;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class AccessoryIngest
{
    private readonly IIntervalSourceRegistry _registry;

    public AccessoryIngest(IIntervalSourceRegistry registry) => _registry = registry;

    public void Apply(TurnoutSnapshot snapshot, DateTimeOffset at)
    {
        var key = KeyFor(snapshot.Address);

        if (snapshot.Position == TurnoutPosition.Unknown)
        {
            if (_registry.Find(key) is AccessorySource known) known.Apply(snapshot.Position, at);
            return;
        }

        _registry.GetOrCreate<AccessorySource>(key, created => created.Address = snapshot.Address)
            .Apply(snapshot.Position, at);
    }

    private string KeyFor(int address) =>
        string.Create(CultureInfo.InvariantCulture, $"accessory:{address}");
}
