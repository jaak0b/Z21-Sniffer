using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Z21Sniffer.UI.Desktop.Views;

public partial class TrackPowerLegendContentView : UserControl
{
    public TrackPowerLegendContentView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
