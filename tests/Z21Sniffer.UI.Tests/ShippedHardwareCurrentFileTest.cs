using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Infrastructure.Persistence;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class ShippedHardwareCurrentFileTest
{
    private JsonStationCurrentLimits _limits = null!;

    [SetUp]
    public void SetUp() =>
        _limits = new JsonStationCurrentLimits(Path.Combine(AppContext.BaseDirectory, "hardware-current.json"));

    [TestCase(512, "Z21 (black)", 3000)]
    [TestCase(513, "Z21 (black)", 3000)]
    [TestCase(515, "z21 (white)", 2000)]
    [TestCase(516, "z21 start", 2000)]
    [TestCase(517, "Z21 Single Booster", 3000)]
    [TestCase(518, "Z21 Dual Booster", 3000)]
    [TestCase(529, "Z21 XL", 6000)]
    [TestCase(530, "Z21 XL Booster", 6000)]
    public void ShippedFile_MapsEachKnownDeviceToItsNameAndRatedCurrent(int typeCode, string name, int milliamps)
    {
        var limit = _limits.Lookup(new StationHardware(typeCode, FirmwareVersion: 0));

        Assert.That(limit, Is.Not.Null, $"hardware {typeCode} is missing from the shipped hardware-current.json");
        Assert.That(limit!.Name, Is.EqualTo(name));
        Assert.That(limit.MaxCurrentMilliamps, Is.EqualTo(milliamps));
    }

    [TestCase(514)]
    [TestCase(769)]
    [TestCase(770)]
    public void ShippedFile_OmitsDevicesWithoutAPublishedRating_SoTheyAutoScale(int typeCode) =>
        Assert.That(_limits.Lookup(new StationHardware(typeCode, FirmwareVersion: 0)), Is.Null);
}
