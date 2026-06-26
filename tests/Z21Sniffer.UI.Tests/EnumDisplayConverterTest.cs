using System.Globalization;
using NUnit.Framework;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.ViewModels;
using Z21Sniffer.UI.Desktop.Converters;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class EnumDisplayConverterTest
{
    private EnumDisplayConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new EnumDisplayConverter();
        LocalizationService.Instance.Apply("en");
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private object Convert(object? value) =>
        _converter.Convert([value, LocalizationService.Instance.CurrentCode], typeof(string), null, CultureInfo.InvariantCulture);

    [Test]
    public void Convert_Theme_ReturnsLocalizedLabel()
    {
        Assert.That(Convert(AppTheme.Dark), Is.EqualTo("Dark"));
        Assert.That(Convert(AppTheme.Light), Is.EqualTo("Light"));
    }

    [Test]
    public void Convert_Language_ReturnsLocalizedLabel()
    {
        Assert.That(Convert(AppLanguage.German), Is.EqualTo("German"));
        Assert.That(Convert(AppLanguage.English), Is.EqualTo("English"));
    }

    [Test]
    public void Convert_AfterLanguageSwitch_ReturnsGermanLabel()
    {
        LocalizationService.Instance.Apply("de");

        Assert.That(Convert(AppTheme.Dark), Is.EqualTo("Dunkel"));
    }

    [Test]
    public void Convert_NonEnum_ReturnsEmpty()
    {
        Assert.That(Convert(null), Is.EqualTo(string.Empty));
        Assert.That(Convert("not an enum"), Is.EqualTo(string.Empty));
    }
}
