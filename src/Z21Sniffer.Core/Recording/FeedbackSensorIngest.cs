using System.Globalization;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public sealed class FeedbackSensorIngest
{
    private readonly IIntervalSourceRegistry _registry;

    public FeedbackSensorIngest(IIntervalSourceRegistry registry) => _registry = registry;

    public event EventHandler<SensorEdge>? EdgeDetected;

    public void Apply(IReadOnlyList<SensorState> states, DateTimeOffset at)
    {
        foreach (var state in states)
        {
            if (state.Occupied) ApplyOccupied(state.Sensor, at);
            else ApplyClear(state.Sensor, at);
        }
    }

    private void ApplyOccupied(SensorKey sensor, DateTimeOffset at)
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>(KeyFor(sensor), created => created.Sensor = sensor);
        if (source.CurrentInterval is not null) return;

        source.Apply(occupied: true, at);
        EdgeDetected?.Invoke(this, new SensorEdge(sensor, Occupied: true, at, source.Label));
    }

    private void ApplyClear(SensorKey sensor, DateTimeOffset at)
    {
        if (_registry.Find(KeyFor(sensor)) is not FeedbackSensorSource source || source.CurrentInterval is null) return;

        source.Apply(occupied: false, at);
        EdgeDetected?.Invoke(this, new SensorEdge(sensor, Occupied: false, at, source.Label));
    }

    private string KeyFor(SensorKey sensor) =>
        string.Create(CultureInfo.InvariantCulture, $"sensor:{sensor.Module}.{sensor.Contact}");
}
