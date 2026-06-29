using CommunityToolkit.Mvvm.ComponentModel;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class LegendRowViewModel : ObservableObject
{
    [ObservableProperty]
    private double _height = 26;

    public LegendRowViewModel(IIntervalSource source, object content, string iconGeometry, bool iconStroked)
    {
        Source = source;
        Content = content;
        IconGeometry = iconGeometry;
        IconStroked = iconStroked;
    }

    public IIntervalSource Source { get; }

    public object Content { get; }

    public string IconGeometry { get; }

    public bool IconStroked { get; }
}
