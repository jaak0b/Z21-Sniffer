using NUnit.Framework;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class AppSettingsTest
{
    [Test]
    public void CaptureTrainData_DefaultsToFalse()
    {
        var settings = new AppSettings("h", 1, "en");

        Assert.That(settings.CaptureTrainData, Is.False);
    }

    [Test]
    public void CaptureTrainData_SetViaWith_CarriesThrough()
    {
        var settings = new AppSettings("h", 1, "en") with { CaptureTrainData = true };

        Assert.That(settings.CaptureTrainData, Is.True);
    }
}
