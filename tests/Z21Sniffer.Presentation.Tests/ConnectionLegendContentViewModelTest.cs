using NUnit.Framework;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class ConnectionLegendContentViewModelTest
{
    private ConnectionSource _source = null!;
    private ConnectionLegendContentViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _source = new ConnectionSource();
        _vm = new ConnectionLegendContentViewModel(_source);
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Label_IsLocalizedConnectionWord()
    {
        LocalizationService.Instance.Apply("en");

        Assert.That(_vm.Label, Is.EqualTo("Connection"));
    }

    [Test]
    public void Details_DescribesTheConnection()
    {
        LocalizationService.Instance.Apply("en");

        Assert.That(_vm.Details, Is.EqualTo("Command station connection"));
    }

    [Test]
    public void Details_IsLocalized()
    {
        LocalizationService.Instance.Apply("de");

        Assert.That(_vm.Details, Is.EqualTo("Verbindung zur Zentrale"));
    }
}
