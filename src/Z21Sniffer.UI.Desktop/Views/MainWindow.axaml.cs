using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
