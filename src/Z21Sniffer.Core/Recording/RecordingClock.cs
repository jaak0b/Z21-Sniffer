using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Core.Recording;

public sealed class RecordingClock : IRecordingClock
{
    private readonly IClock _inner;
    private DateTimeOffset _frozen;

    public RecordingClock(IClock inner)
    {
        _inner = inner;
        _frozen = inner.Now;
    }

    public bool IsRunning { get; private set; }

    public DateTimeOffset Now => IsRunning ? _inner.Now : _frozen;

    public event EventHandler? RunningChanged;

    public void Start()
    {
        if (IsRunning) return;

        IsRunning = true;
        RunningChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _frozen = _inner.Now;
        IsRunning = false;
        RunningChanged?.Invoke(this, EventArgs.Empty);
    }
}
