using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class RangeInputBox : UserControl
{
    public static readonly StyledProperty<double> MinValueProperty =
        AvaloniaProperty.Register<RangeInputBox, double>(nameof(MinValue), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MaxValueProperty =
        AvaloniaProperty.Register<RangeInputBox, double>(nameof(MaxValue), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<RangeInputBox, string?>(nameof(Unit));

    public static readonly StyledProperty<string?> SeparatorProperty =
        AvaloniaProperty.Register<RangeInputBox, string?>(nameof(Separator));

    private readonly NumericTextConverter _converter = new();

    public RangeInputBox() =>
        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Field(MinValueProperty),
                Word(SeparatorProperty),
                Field(MaxValueProperty),
                Word(UnitProperty)
            }
        };

    public double MinValue
    {
        get => GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public double MaxValue
    {
        get => GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string? Separator
    {
        get => GetValue(SeparatorProperty);
        set => SetValue(SeparatorProperty, value);
    }

    private TextBox Field(StyledProperty<double> value)
    {
        var input = new TextBox { Width = 56, TextAlignment = TextAlignment.Center };
        input.Bind(TextBox.TextProperty, new Binding(value.Name) { Source = this, Mode = BindingMode.TwoWay, Converter = _converter });
        return input;
    }

    private TextBlock Word(StyledProperty<string?> text)
    {
        var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        label[!TextBlock.TextProperty] = new Binding(text.Name) { Source = this };
        label[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextSecondaryBrush");
        return label;
    }
}
