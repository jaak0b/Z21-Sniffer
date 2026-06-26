using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Model;

public sealed record RecordingSession(DateTimeOffset StartedAt, IReadOnlyList<IIntervalSource> Sources);
