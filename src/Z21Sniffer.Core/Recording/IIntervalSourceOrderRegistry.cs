namespace Z21Sniffer.Core.Recording;

public interface IIntervalSourceOrderRegistry
{
    int IndexOf(string id);

    void Register(string id);

    void Insert(string id, string? afterId);

    void Reorder(IReadOnlyList<string> orderedIds);

    void Clear();
}
