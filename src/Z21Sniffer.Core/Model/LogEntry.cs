namespace Z21Sniffer.Core.Model;

public sealed record LogEntry(DateTimeOffset Timestamp, LogEntryKind Kind, string Message, bool Fault = false);
