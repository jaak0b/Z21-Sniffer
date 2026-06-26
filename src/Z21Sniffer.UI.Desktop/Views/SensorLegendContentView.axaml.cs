using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class SensorLegendContentView : UserControl
{
    public SensorLegendContentView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnLostFocus(object? sender, RoutedEventArgs e) => Commit();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Commit();
    }

    private void Commit()
    {
        if (DataContext is SensorLegendContentViewModel vm) vm.CommitRenameCommand.Execute(null);
    }
}
