using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class SystemCurrentLegendContentView : UserControl
{
    public SystemCurrentLegendContentView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
