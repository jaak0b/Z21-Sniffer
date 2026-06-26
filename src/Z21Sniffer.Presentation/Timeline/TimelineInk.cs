namespace Z21Sniffer.Presentation.Timeline;

public readonly record struct TimelineInk(string Key);

public static class TimelineInkKeys
{
    public const string Bar = "Bar";

    public const string HighlightedBar = "HighlightedBar";

    public const string HighlightOutline = "HighlightOutline";

    public const string BarText = "BarText";

    public const string StoppedFlag = "StoppedFlag";

    public const string Connected = "Connected";

    public const string Disconnected = "Disconnected";

    public const string ConnectionText = "ConnectionText";
}
