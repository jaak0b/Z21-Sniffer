namespace Z21Sniffer.Core.Model;

public enum IntervalEndReason
{
    Open,
    FallingEdge,
    Stopped,
}

public interface IInterval
{
    string Key { get; set; }

    DateTimeOffset Start { get; set; }

    DateTimeOffset? End { get; set; }

    IntervalEndReason EndReason { get; set; }

    bool IsOpen { get; }

    TimeSpan? Duration { get; }
}

public abstract class IntervalBase : IInterval
{
    public string Key { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset? End { get; set; }

    public IntervalEndReason EndReason { get; set; }

    public bool IsOpen => End is null;

    public TimeSpan? Duration => End is { } end ? end - Start : null;
}
