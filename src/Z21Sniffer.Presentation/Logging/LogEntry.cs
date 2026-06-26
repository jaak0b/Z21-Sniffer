namespace Z21Sniffer.Presentation.Logging;

public sealed record LogEntry(DateTimeOffset Timestamp, LogEntryKind Kind, string Message, bool Fault = false);
