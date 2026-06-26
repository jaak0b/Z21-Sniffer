using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.UI.Desktop;

public sealed class AvaloniaThemeController : IThemeController
{
    private const string Root = "avares://Z21Sniffer.UI.Desktop/Themes";

    private ResourceInclude? _palette;

    public void Apply(bool dark)
    {
        var app = Application.Current;
        if (app is null) return;

        var uri = new Uri($"{Root}/Colors.{(dark ? "Graphite" : "Light")}.axaml");
        var dictionary = new ResourceInclude(uri) { Source = uri };
        var merged = app.Resources.MergedDictionaries;

        if (_palette is not null && merged.Contains(_palette))
            merged[merged.IndexOf(_palette)] = dictionary;
        else
            merged.Insert(0, dictionary);

        _palette = dictionary;
        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
