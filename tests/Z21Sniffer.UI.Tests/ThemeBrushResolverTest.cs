using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Styling;
using NUnit.Framework;
using Z21Sniffer.UI.Desktop.Controls;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class ThemeBrushResolverTest
{
    private readonly ThemeBrushResolver _resolver = new();

    [AvaloniaTest]
    public void Resolve_ForAKnownThemeKey_ReturnsThatThemeBrush()
    {
        var expected = new SolidColorBrush(Colors.Red);
        var host = new Border { Resources = { ["MyBrush"] = expected } };

        Assert.That(_resolver.Resolve(host, ThemeVariant.Light, "MyBrush"), Is.SameAs(expected));
    }

    [AvaloniaTest]
    public void Resolve_ForAMissingKey_ReturnsTransparentRatherThanAHardcodedColor() =>
        Assert.That(_resolver.Resolve(new Border(), ThemeVariant.Light, "NoSuchBrushKey"), Is.SameAs(Brushes.Transparent));
}
