using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class UnitInputBox : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<UnitInputBox, double>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<UnitInputBox, string?>(nameof(Unit));

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<UnitInputBox, string?>(nameof(Watermark));

    public UnitInputBox()
    {
        var input = new TextBox
        {
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 4, 24, 4)
        };
        input.Bind(TextBox.TextProperty, new Binding(nameof(Value)) { Source = this, Mode = BindingMode.TwoWay });
        input.Bind(TextBox.PlaceholderTextProperty, new Binding(nameof(Watermark)) { Source = this });

        var suffix = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Opacity = 0.6,
            IsHitTestVisible = false
        };
        suffix.Bind(TextBlock.TextProperty, new Binding(nameof(Unit)) { Source = this });

        Content = new Grid { Children = { input, suffix } };
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
}
