namespace Z21Sniffer.Core.Model;

public sealed record LogLine(DateTimeOffset Timestamp, string Kind, string Message);
