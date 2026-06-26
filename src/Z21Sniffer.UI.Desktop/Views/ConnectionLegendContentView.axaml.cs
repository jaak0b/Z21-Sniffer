using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class ConnectionLegendContentView : UserControl
{
    public ConnectionLegendContentView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
