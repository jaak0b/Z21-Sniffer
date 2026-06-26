using System.Globalization;
using Avalonia.Data.Converters;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Logging;

namespace Z21Sniffer.UI.Desktop.Converters;

public sealed class KindToLocalizedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is LogEntryKind kind ? LocalizationService.Instance["Kind" + kind] : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
