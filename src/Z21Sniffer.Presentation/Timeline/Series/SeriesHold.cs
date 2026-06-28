namespace Z21Sniffer.Presentation.Timeline.Series;

public sealed class SeriesHold
{
    public int LastIndexAtOrBefore<T>(IReadOnlyList<T> items, DateTimeOffset at, Func<T, DateTimeOffset> timeOf)
    {
        var found = -1;
        for (var index = 0; index < items.Count; index++)
        {
            if (timeOf(items[index]) > at) break;
            found = index;
        }

        return found;
    }
}
