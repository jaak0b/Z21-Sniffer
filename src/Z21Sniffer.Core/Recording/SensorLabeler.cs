using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class SensorLabeler
{
    public string Label(SensorKey sensor, IReadOnlyList<SensorAlias> aliases)
    {
        var alias = aliases.FirstOrDefault(a => a.Sensor == sensor);
        return alias is not null && !string.IsNullOrWhiteSpace(alias.Name)
            ? alias.Name
            : $"M{sensor.Module}.{sensor.Contact}";
    }
}
