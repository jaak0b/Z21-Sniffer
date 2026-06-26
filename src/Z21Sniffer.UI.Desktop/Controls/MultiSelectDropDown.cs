using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Z21Sniffer.Presentation.Controls;
using Z21Sniffer.Presentation.Localization;

namespace Z21Sniffer.UI.Desktop.Controls;

public sealed class MultiSelectDropDown : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MultiSelectDropDown, string?>(nameof(Title));

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MultiSelectDropDown, IEnumerable?>(nameof(ItemsSource));

    private readonly Button _button;

    public MultiSelectDropDown()
    {
        _button = new Button { HorizontalAlignment = HorizontalAlignment.Stretch };
        _button.Bind(ContentControl.ContentProperty, this.GetObservable(TitleProperty));
        _button.Click += (_, _) => ShowFlyout();
        Content = _button;
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public void SelectAll() => SetAll(true);

    public void DeselectAll() => SetAll(false);

    private void SetAll(bool selected)
    {
        if (ItemsSource is null) return;
        foreach (var option in ItemsSource.OfType<ISelectableOption>()) option.IsSelected = selected;
    }

    private void ShowFlyout()
    {
        var localization = LocalizationService.Instance;

        var selectAll = new Button { Content = localization["SelectAll"] };
        selectAll.Click += (_, _) => SelectAll();
        var deselectAll = new Button { Content = localization["DeselectAll"] };
        deselectAll.Click += (_, _) => DeselectAll();

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        actions.Children.Add(selectAll);
        actions.Children.Add(deselectAll);

        var list = new ItemsControl
        {
            ItemsSource = ItemsSource,
            ItemTemplate = new FuncDataTemplate<ISelectableOption>((_, _) =>
            {
                var check = new CheckBox { Margin = new Thickness(0, 2, 0, 0) };
                check.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ISelectableOption.IsSelected)) { Mode = BindingMode.TwoWay });
                check.Bind(ContentControl.ContentProperty, new Binding(nameof(ISelectableOption.Label)));
                return check;
            })
        };

        var panel = new StackPanel { Spacing = 6, MinWidth = 180 };
        panel.Children.Add(actions);
        panel.Children.Add(new ScrollViewer { Content = list, MaxHeight = 320 });

        new Flyout { Content = panel }.ShowAt(_button);
    }
}
