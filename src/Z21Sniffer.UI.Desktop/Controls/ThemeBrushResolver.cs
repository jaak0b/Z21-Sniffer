using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class ThemeBrushResolver
{
    public IBrush Resolve(IResourceHost host, ThemeVariant variant, string key) =>
        host.TryFindResource(key, variant, out var value) && value is IBrush brush
            ? brush
            : Brushes.Transparent;
}
