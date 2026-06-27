using CommunityToolkit.Mvvm.ComponentModel;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed partial class LegendRowViewModel : ObservableObject
{
    [ObservableProperty]
    private double _height = 26;

    public LegendRowViewModel(IIntervalSource source, object content)
    {
        Source = source;
        Content = content;
    }

    public IIntervalSource Source { get; }

    public object Content { get; }
}
