using NUnit.Framework;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class SystemCurrentLegendContentViewModelTest
{
    [SetUp]
    public void SetUp() => LocalizationService.Instance.Apply("en");

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Label_IsTheLocalizedSystemCurrentName()
    {
        var vm = new SystemCurrentLegendContentViewModel(new SystemCurrentSource { Id = "systemcurrent" });

        Assert.That(vm.Label, Is.EqualTo("System current"));
    }

    [Test]
    public void Details_IsTheLocalizedDescription()
    {
        var vm = new SystemCurrentLegendContentViewModel(new SystemCurrentSource { Id = "systemcurrent" });

        Assert.That(vm.Details, Is.EqualTo("Booster current draw"));
    }

    [Test]
    public void Source_ExposesTheGivenSource()
    {
        var source = new SystemCurrentSource { Id = "systemcurrent" };
        var vm = new SystemCurrentLegendContentViewModel(source);

        Assert.That(vm.Source, Is.SameAs(source));
    }
}
