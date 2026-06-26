using CommandStation.Model;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Infrastructure.Z21;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class FeedbackDecoderTest
{
    private readonly FeedbackDecoder _decoder = new();

    [Test]
    public void Decode_Group0FirstModule_MapsToModule1Contacts1To8()
    {
        var result = _decoder.Decode(new FeedbackData(GroupIndex: 0, States: [0b0000_0000]));

        Assert.That(result, Has.Count.EqualTo(8));
        Assert.That(result.Select(s => s.Sensor.Module), Is.All.EqualTo(1));
        Assert.That(result.Select(s => s.Sensor.Contact), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public void Decode_GroupIndexOffsetsModuleAddress()
    {
        var result = _decoder.Decode(new FeedbackData(GroupIndex: 1, States: [0, 0]));

        Assert.That(result.Where(s => s.Sensor.Contact == 1).Select(s => s.Sensor.Module),
            Is.EqualTo(new[] { 11, 12 }));
    }

    [Test]
    public void Decode_BitSet_MarksMatchingContactOccupied()
    {
        var result = _decoder.Decode(new FeedbackData(GroupIndex: 0, States: [0b0000_0101]));

        var module1 = result.Where(s => s.Sensor.Module == 1).ToList();
        Assert.That(module1.Single(s => s.Sensor.Contact == 1).Occupied, Is.True);
        Assert.That(module1.Single(s => s.Sensor.Contact == 2).Occupied, Is.False);
        Assert.That(module1.Single(s => s.Sensor.Contact == 3).Occupied, Is.True);
    }

    [Test]
    public void Decode_EmptyStates_ReturnsEmpty()
    {
        Assert.That(_decoder.Decode(new FeedbackData(GroupIndex: 0, States: [])), Is.Empty);
    }
}
