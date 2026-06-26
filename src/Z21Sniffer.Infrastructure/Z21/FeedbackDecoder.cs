using CommandStation.Model;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Infrastructure.Z21;

public sealed class FeedbackDecoder
{
    public IReadOnlyList<SensorState> Decode(FeedbackData data)
    {
        var states = new List<SensorState>(data.States.Count * 8);
        for (var moduleIndex = 0; moduleIndex < data.States.Count; moduleIndex++)
        {
            var module = data.GroupIndex * 10 + moduleIndex + 1;
            var bits = data.States[moduleIndex];
            for (var bit = 0; bit < 8; bit++)
            {
                var occupied = (bits & (1 << bit)) != 0;
                states.Add(new SensorState(new SensorKey(module, bit + 1), occupied));
            }
        }

        return states;
    }
}
