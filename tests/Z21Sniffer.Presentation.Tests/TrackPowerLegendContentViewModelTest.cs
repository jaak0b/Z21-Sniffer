using NUnit.Framework;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TrackPowerLegendContentViewModelTest
{
    private TrackPowerSource _source = null!;
    private TrackPowerLegendContentViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _source = new TrackPowerSource();
        _vm = new TrackPowerLegendContentViewModel(_source);
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Label_IsLocalizedTrackPowerWord()
    {
        LocalizationService.Instance.Apply("en");

        Assert.That(_vm.Label, Is.EqualTo("Track power"));
    }

    [Test]
    public void Details_DescribesTheTrackPower()
    {
        LocalizationService.Instance.Apply("en");

        Assert.That(_vm.Details, Is.EqualTo("Command station track power"));
    }

    [Test]
    public void Details_IsLocalized()
    {
        LocalizationService.Instance.Apply("de");

        Assert.That(_vm.Details, Is.EqualTo("Gleisspannung der Zentrale"));
    }
}
