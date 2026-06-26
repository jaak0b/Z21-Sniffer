namespace Z21Sniffer.Core.Ports;

public interface IRecordingClock : IClock
{
    bool IsRunning { get; }

    void Start();

    void Stop();

    event EventHandler? RunningChanged;
}
