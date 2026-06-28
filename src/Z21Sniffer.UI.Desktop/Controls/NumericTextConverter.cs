using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class NumericTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double number ? number.ToString(culture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return 0d;
        return double.TryParse(text, NumberStyles.Float, culture, out var number) ? number : BindingOperations.DoNothing;
    }
}
