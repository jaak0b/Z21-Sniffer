using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Presentation.Logging;

namespace Z21Sniffer.UI.Desktop.Converters;

public sealed class KindToBrushConverter : IValueConverter
{
    private readonly LogKindStyle _style = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LogEntryKind kind) return Brushes.Transparent;

        var app = Application.Current;
        if (app is not null && app.TryGetResource(_style.ResourceKey(kind), app.ActualThemeVariant, out var brush))
            return brush;

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
