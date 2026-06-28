using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.FindControl<TextBlock>("DebugBadge")!.IsVisible = true;
#endif
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
