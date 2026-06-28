using System.Text.Json;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Infrastructure.Persistence;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class JsonStationCurrentLimitsTest
{
    private string _directory = null!;
    private string _filePath = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "z21-limits-" + Guid.NewGuid().ToString("N"));
        _filePath = Path.Combine(_directory, "hardware-current.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void MaxCurrentMilliamps_ForAKnownStation_ReturnsItsRatedCurrent()
    {
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.MaxCurrentMilliamps(new StationHardware(TypeCode: 529, FirmwareVersion: 0)), Is.EqualTo(7000));
    }

    [Test]
    public void MaxCurrentMilliamps_ForAnUnknownStation_FallsBackToTheDefault()
    {
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.MaxCurrentMilliamps(new StationHardware(TypeCode: 99999, FirmwareVersion: 0)), Is.EqualTo(3200));
    }

    [Test]
    public void Construction_WhenNoFileExists_SeedsItWithTheDefaultTable()
    {
        _ = new JsonStationCurrentLimits(_filePath);
        _ = new JsonStationCurrentLimits(_filePath).MaxCurrentMilliamps(new StationHardware(513, 0));

        Assert.That(File.Exists(_filePath), Is.True);
    }

    [Test]
    public void MaxCurrentMilliamps_HonoursAnOwnerEditedFileOverTheDefault()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(new Dictionary<string, int> { ["Z21New"] = 4096 }));
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.MaxCurrentMilliamps(new StationHardware(TypeCode: 513, FirmwareVersion: 0)), Is.EqualTo(4096));
    }

    [Test]
    public void MaxCurrentMilliamps_WhenTheFileIsCorrupt_FallsBackToTheDefaultTable()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, "{ this is not valid json");
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.MaxCurrentMilliamps(new StationHardware(TypeCode: 529, FirmwareVersion: 0)), Is.EqualTo(7000));
    }
}
