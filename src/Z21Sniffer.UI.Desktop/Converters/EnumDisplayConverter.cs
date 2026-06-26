using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.UI.Desktop.Converters;

public sealed class EnumDisplayConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count > 0 && values[0] is Enum value
            ? LocalizationService.Instance[value.ToString()]
            : string.Empty;
}
