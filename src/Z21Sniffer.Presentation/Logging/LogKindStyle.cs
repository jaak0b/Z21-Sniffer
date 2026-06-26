namespace Z21Sniffer.Presentation.Logging;

public sealed class LogKindStyle
{
    public string ResourceKey(LogEntryKind kind) => kind switch
    {
        LogEntryKind.Connection => "BorderStrongBrush",
        LogEntryKind.TrackPower => "WarningBrush",
        LogEntryKind.Sensor => "PrimaryBrush",
        LogEntryKind.System => "DangerBrush",
        LogEntryKind.Loco => "PrimaryHoverBrush",
        LogEntryKind.Turnout => "TextSecondaryBrush",
        _ => "BorderBrush"
    };
}
