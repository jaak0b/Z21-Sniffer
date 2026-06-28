using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public interface IIntervalSource
{
    string Id { get; set; }

    Type IntervalType { get; }

    bool HighlightsShortIntervals { get; }

    int Order { get; set; }

    IReadOnlyList<IInterval> Intervals { get; }

    void UsePersistence(IKeyValueStore store);

    void SeedOrder(int order);

    void CloseOpenIntervals(DateTimeOffset at, IntervalEndReason reason);

    void Clear();

    event EventHandler? Changed;
}

public interface IIntervalSource<T> : IIntervalSource where T : IInterval
{
    new IReadOnlyList<T> Intervals { get; }

    void Upsert(T interval);
}
