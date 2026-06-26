using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class SensorLabelerTest
{
    private readonly SensorLabeler _labeler = new();

    [Test]
    public void Label_WithMatchingAlias_ReturnsAliasName()
    {
        var sensor = new SensorKey(3, 5);
        var aliases = new[] { new SensorAlias(sensor, "Station track 2") };

        Assert.That(_labeler.Label(sensor, aliases), Is.EqualTo("Station track 2"));
    }

    [Test]
    public void Label_WithoutAlias_ReturnsCompactDefault()
    {
        Assert.That(_labeler.Label(new SensorKey(3, 5), []), Is.EqualTo("M3.5"));
    }

    [Test]
    public void Label_WithBlankAlias_FallsBackToDefault()
    {
        var sensor = new SensorKey(3, 5);
        var aliases = new[] { new SensorAlias(sensor, "   ") };

        Assert.That(_labeler.Label(sensor, aliases), Is.EqualTo("M3.5"));
    }
}
