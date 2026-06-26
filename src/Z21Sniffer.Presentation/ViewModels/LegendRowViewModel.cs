using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Presentation.ViewModels;

public sealed class LegendRowViewModel
{
    public LegendRowViewModel(IIntervalSource source, object content)
    {
        Source = source;
        Content = content;
    }

    public IIntervalSource Source { get; }

    public object Content { get; }
}
