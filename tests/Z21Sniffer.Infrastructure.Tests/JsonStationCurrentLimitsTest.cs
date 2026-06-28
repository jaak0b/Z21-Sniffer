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

    private void WriteFile(params (int Id, string Name, int Milliamps)[] entries)
    {
        Directory.CreateDirectory(_directory);
        var payload = entries.Select(entry => new { hardwareId = entry.Id, name = entry.Name, maxCurrentMilliamps = entry.Milliamps });
        File.WriteAllText(_filePath, JsonSerializer.Serialize(payload));
    }

    [Test]
    public void Lookup_ForAKnownStation_ReturnsItsNameAndRatedCurrent()
    {
        WriteFile((529, "Z21 XL", 6000), (516, "z21 start", 2000));
        var limits = new JsonStationCurrentLimits(_filePath);

        var limit = limits.Lookup(new StationHardware(TypeCode: 529, FirmwareVersion: 0));

        Assert.That(limit, Is.Not.Null);
        Assert.That(limit!.Name, Is.EqualTo("Z21 XL"));
        Assert.That(limit.MaxCurrentMilliamps, Is.EqualTo(6000));
    }

    [Test]
    public void Lookup_ForAnUnknownStation_IsNull()
    {
        WriteFile((529, "Z21 XL", 6000));
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.Lookup(new StationHardware(TypeCode: 99999, FirmwareVersion: 0)), Is.Null);
    }

    [Test]
    public void Lookup_WhenTheFileIsMissing_IsNullAndDoesNotCreateIt()
    {
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.Lookup(new StationHardware(TypeCode: 529, FirmwareVersion: 0)), Is.Null);
        Assert.That(File.Exists(_filePath), Is.False);
    }

    [Test]
    public void Lookup_WhenTheFileIsCorrupt_IsNull()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, "{ this is not valid json");
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.Lookup(new StationHardware(TypeCode: 529, FirmwareVersion: 0)), Is.Null);
    }

    [Test]
    public void Lookup_WhenTheFileContentIsJsonNull_IsNull()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, "null");
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.Lookup(new StationHardware(529, 0)), Is.Null);
    }

    [Test]
    public void Lookup_SkipsNullArrayEntriesAndStillReadsTheRest()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, """[ null, { "hardwareId": 529, "name": "Z21 XL", "maxCurrentMilliamps": 6000 } ]""");
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.Lookup(new StationHardware(529, 0))!.MaxCurrentMilliamps, Is.EqualTo(6000));
    }

    [Test]
    public void Lookup_ReadsFieldNamesCaseInsensitively()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, """[ { "HardwareId": 513, "Name": "Z21 (black)", "MaxCurrentMilliamps": 3000 } ]""");
        var limits = new JsonStationCurrentLimits(_filePath);

        Assert.That(limits.Lookup(new StationHardware(513, 0))!.MaxCurrentMilliamps, Is.EqualTo(3000));
    }
}
