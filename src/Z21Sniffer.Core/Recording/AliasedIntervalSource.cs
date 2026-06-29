using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Recording;

public interface IAliasedSource : IIntervalSource
{
    string Label { get; set; }
}

public abstract class AliasedIntervalSource<T> : IntervalSourceBase<T>, IAliasedSource where T : class, IInterval, new()
{
    protected abstract string DefaultLabel { get; }

    public string Label
    {
        get => Persistence.GetValue($"{Id}/label", DefaultLabel) ?? string.Empty;
        set => Persistence.SetValue($"{Id}/label", value);
    }
}
