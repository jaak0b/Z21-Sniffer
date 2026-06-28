using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Styling;
using NUnit.Framework;
using Z21Sniffer.UI.Desktop;

namespace Z21Sniffer.UI.Tests;

[TestFixture]
public class LightThemePaletteTest
{
    [AvaloniaTest]
    public void SurfaceAltColor_IsDistinctFromBackground_SoNeutralBarsRemainVisible()
    {
        new AvaloniaThemeController().Apply(dark: false);

        Assert.That(Color("SurfaceAltColor", ThemeVariant.Light), Is.Not.EqualTo(Color("BackgroundColor", ThemeVariant.Light)));
    }

    [AvaloniaTest]
    public void GraphiteSurfaceAltColor_StaysDistinctFromBackground()
    {
        new AvaloniaThemeController().Apply(dark: true);

        Assert.That(Color("SurfaceAltColor", ThemeVariant.Dark), Is.Not.EqualTo(Color("BackgroundColor", ThemeVariant.Dark)));
    }

    [AvaloniaTest]
    public void LocoSpeedLineColor_ContrastsAgainstTheNeutralLocoBar()
    {
        new AvaloniaThemeController().Apply(dark: false);

        Assert.That(Color("LocoSpeedLineColor", ThemeVariant.Light), Is.Not.EqualTo(Color("SurfaceAltColor", ThemeVariant.Light)));
        Assert.That(Color("LocoSpeedLineColor", ThemeVariant.Light), Is.Not.EqualTo(Color("BackgroundColor", ThemeVariant.Light)));
    }

    [AvaloniaTest]
    public void SensorBarColor_IsDistinctFromThePowerAndConnectionBlue_InBothThemes()
    {
        new AvaloniaThemeController().Apply(dark: false);
        Assert.That(Color("SensorBarColor", ThemeVariant.Light), Is.Not.EqualTo(Color("PrimaryColor", ThemeVariant.Light)));

        new AvaloniaThemeController().Apply(dark: true);
        Assert.That(Color("SensorBarColor", ThemeVariant.Dark), Is.Not.EqualTo(Color("PrimaryColor", ThemeVariant.Dark)));
    }

    [AvaloniaTest]
    public void SensorBarTextColor_ContrastsWithThePaleSensorBarInLight()
    {
        new AvaloniaThemeController().Apply(dark: false);

        Assert.That(Color("TextPrimaryColor", ThemeVariant.Light), Is.Not.EqualTo(Color("SensorBarColor", ThemeVariant.Light)));
    }

    [AvaloniaTest]
    public void GraphiteLocoSpeedLineColor_StaysWhiteSoDarkModeIsUnchanged()
    {
        new AvaloniaThemeController().Apply(dark: true);

        Assert.That(Color("LocoSpeedLineColor", ThemeVariant.Dark), Is.EqualTo(Colors.White));
    }

    private Color Color(string key, ThemeVariant variant) =>
        Application.Current!.Resources.TryGetResource(key, variant, out var value) && value is Color color
            ? color
            : throw new AssertionException($"Missing color resource '{key}'.");
}
