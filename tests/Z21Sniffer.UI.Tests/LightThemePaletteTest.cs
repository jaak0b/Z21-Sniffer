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

    [AvaloniaTest]
    public void TimelineLaneColor_IsDistinctFromBackgroundInLight()
    {
        new AvaloniaThemeController().Apply(dark: false);

        Assert.That(Color("TimelineLaneColor", ThemeVariant.Light), Is.Not.EqualTo(Color("BackgroundColor", ThemeVariant.Light)));
    }

    [AvaloniaTest]
    public void TimelineLaneColor_DarkMatchesTheOldNeutralBar_SoDarkIsUnchanged()
    {
        new AvaloniaThemeController().Apply(dark: true);

        Assert.That(Color("TimelineLaneColor", ThemeVariant.Dark), Is.EqualTo(Color("SurfaceAltColor", ThemeVariant.Dark)));
    }

    [AvaloniaTest]
    public void TimelineLaneBorderColor_IsVisibleAgainstTheLaneInLight()
    {
        new AvaloniaThemeController().Apply(dark: false);

        Assert.That(Color("TimelineLaneBorderColor", ThemeVariant.Light), Is.Not.EqualTo(Color("TimelineLaneColor", ThemeVariant.Light)));
    }

    [AvaloniaTest]
    public void TimelineLaneBorderColor_DarkIsInvisible_SoDarkIsUnchanged()
    {
        new AvaloniaThemeController().Apply(dark: true);

        Assert.That(Color("TimelineLaneBorderColor", ThemeVariant.Dark), Is.EqualTo(Color("TimelineLaneColor", ThemeVariant.Dark)));
    }

    [AvaloniaTest]
    public void TimelineGridLineColor_IsDistinctFromBackgroundInLight()
    {
        new AvaloniaThemeController().Apply(dark: false);

        Assert.That(Color("TimelineGridLineColor", ThemeVariant.Light), Is.Not.EqualTo(Color("BackgroundColor", ThemeVariant.Light)));
    }

    [AvaloniaTest]
    public void TimelineGridLineColor_DarkMatchesTheOldBorder_SoDarkIsUnchanged()
    {
        new AvaloniaThemeController().Apply(dark: true);

        Assert.That(Color("TimelineGridLineColor", ThemeVariant.Dark), Is.EqualTo(Color("BorderColor", ThemeVariant.Dark)));
    }

    [AvaloniaTest]
    public void TimelineCursorColor_IsDarkerThanTheGridLineInLight()
    {
        new AvaloniaThemeController().Apply(dark: false);

        Assert.That(Color("TimelineCursorColor", ThemeVariant.Light), Is.Not.EqualTo(Color("TimelineGridLineColor", ThemeVariant.Light)));
        Assert.That(Color("TimelineCursorColor", ThemeVariant.Light), Is.Not.EqualTo(Color("BackgroundColor", ThemeVariant.Light)));
    }

    [AvaloniaTest]
    public void TimelineCursorColor_DarkMatchesTheOldBorder_SoDarkIsUnchanged()
    {
        new AvaloniaThemeController().Apply(dark: true);

        Assert.That(Color("TimelineCursorColor", ThemeVariant.Dark), Is.EqualTo(Color("BorderColor", ThemeVariant.Dark)));
    }

    private Color Color(string key, ThemeVariant variant) =>
        Application.Current!.Resources.TryGetResource(key, variant, out var value) && value is Color color
            ? color
            : throw new AssertionException($"Missing color resource '{key}'.");
}
