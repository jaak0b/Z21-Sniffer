using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        var sourceCombo = this.FindControl<ComboBox>("SourceCombo");
        if (sourceCombo is not null)
            sourceCombo.ItemsSource = new[] { ConnectionSourceType.Z21, ConnectionSourceType.Simulation };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
