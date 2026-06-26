using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class SensorBarTextTest
{
    private readonly SensorBarText _text = new();

    [Test]
    public void Describe_WithAlias_ShowsLabelAddressAndOnTime()
    {
        var result = _text.Describe("Station track 2", new SensorKey(3, 5), TimeSpan.FromMilliseconds(40));

        Assert.That(result, Is.EqualTo("Station track 2 (M3.5) · on 0.04 s"));
    }

    [Test]
    public void Describe_WhenLabelIsTheAddress_DoesNotDuplicateIt()
    {
        var result = _text.Describe("M3.5", new SensorKey(3, 5), TimeSpan.FromSeconds(4));

        Assert.That(result, Is.EqualTo("M3.5 · on 4 s"));
    }

    [Test]
    public void Describe_FormatsFractionalSeconds()
    {
        var result = _text.Describe("M1.1", new SensorKey(1, 1), TimeSpan.FromMilliseconds(1500));

        Assert.That(result, Is.EqualTo("M1.1 · on 1.5 s"));
    }
}
