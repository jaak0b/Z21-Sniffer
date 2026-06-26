using Avalonia.Headless.NUnit;
using Avalonia.Styling;
using NUnit.Framework;
using Z21Sniffer.UI.Desktop;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class AvaloniaThemeControllerTest
{
    [AvaloniaTest]
    public void Apply_SwapsThemeVariantWithoutThrowing()
    {
        var controller = new AvaloniaThemeController();

        controller.Apply(dark: true);
        Assert.That(Avalonia.Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Dark));

        controller.Apply(dark: false);
        Assert.That(Avalonia.Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Light));
    }
}
