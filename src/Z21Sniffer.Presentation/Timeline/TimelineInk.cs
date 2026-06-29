namespace Z21Sniffer.Presentation.Timeline;

public readonly record struct TimelineInk(string Key);

public static class TimelineInkKeys
{
    public const string Bar = "Bar";

    public const string HighlightOutline = "HighlightOutline";

    public const string BarText = "BarText";

    public const string StoppedFlag = "StoppedFlag";

    public const string Connected = "Connected";

    public const string Disconnected = "Disconnected";

    public const string ConnectionText = "ConnectionText";

    public const string LocoBar = "LocoBar";

    public const string LocoSpeedLine = "LocoSpeedLine";

    public const string LocoBaseline = "LocoBaseline";

    public const string LocoText = "LocoText";

    public const string TrackPowerOn = "TrackPowerOn";

    public const string TrackPowerOff = "TrackPowerOff";

    public const string TrackPowerShort = "TrackPowerShort";

    public const string TrackPowerProgramming = "TrackPowerProgramming";

    public const string TrackPowerText = "TrackPowerText";

    public const string TrackPowerOffText = "TrackPowerOffText";

    public const string SystemCurrentBar = "SystemCurrentBar";

    public const string SystemCurrentLine = "SystemCurrentLine";

    public const string SystemCurrentBaseline = "SystemCurrentBaseline";

    public const string SystemCurrentText = "SystemCurrentText";

    public const string AccessoryOutput1 = "AccessoryOutput1";

    public const string AccessoryOutput2 = "AccessoryOutput2";

    public const string AccessoryText = "AccessoryText";
}
