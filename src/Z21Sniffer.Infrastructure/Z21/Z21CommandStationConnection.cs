using System.Net;
using CommandStation.Model;
using CommandStation.Transport;
using CommandStation.Transport.Udp;
using Z21.Core;
using Z21.Core.Command.SystemState;
using Z21.Core.Model;
using Z21.Core.Model.EventArgs;
using Z21.Core.ResponseHandler.SystemState;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;

namespace Z21Sniffer.Infrastructure.Z21;

public sealed class Z21CommandStationConnection : ICommandStationConnection
{
    private readonly IZ21CommandStation _station;
    private readonly UdpTransportOptions _options;
    private readonly FeedbackDecoder _decoder;
    private readonly Z21SnapshotMapper _mapper;
    private readonly IBroadcastFlagsResponseHandler _broadcastFlagsReadback;
    private readonly IHardwareInfoResponseHandler _hardwareInfoReadback;

    private readonly uint[] _broadcastFlags =
    [
        Z21BroadcastFlags.RmBusDataChangedMessages,
        Z21BroadcastFlags.SystemStateDataChangedMessages,
        Z21BroadcastFlags.DriveAndSwitchingMessages,
        Z21BroadcastFlags.LocoInfoChangedMessages,
    ];

    public Z21CommandStationConnection(
        IZ21CommandStation station,
        UdpTransportOptions options,
        FeedbackDecoder decoder,
        Z21SnapshotMapper mapper,
        IBroadcastFlagsResponseHandler broadcastFlagsReadback,
        IHardwareInfoResponseHandler hardwareInfoReadback)
    {
        _station = station;
        _options = options;
        _decoder = decoder;
        _mapper = mapper;
        _broadcastFlagsReadback = broadcastFlagsReadback;
        _hardwareInfoReadback = hardwareInfoReadback;

        _station.FeedbackChanged += OnFeedbackChanged;
        _station.TrackPowerChanged += OnTrackPowerChanged;
        _station.ConnectionChanged += OnConnectionChanged;
        _station.SystemStateReceived += OnSystemStateReceived;
        _station.LocoInfoReceived += OnLocoInfoReceived;
        _station.TurnoutInfoReceived += OnTurnoutInfoReceived;
        _hardwareInfoReadback.OnHardwareInfoReceived += OnHardwareInfoReceived;
    }

    public event EventHandler<IReadOnlyList<SensorState>>? FeedbackReceived;

    public event EventHandler<bool>? ConnectionChanged;

    public event EventHandler<bool>? TrackPowerChanged;

    public event EventHandler<SystemSnapshot>? SystemStateReceived;

    public event EventHandler<LocoSnapshot>? LocoInfoReceived;

    public event EventHandler<TurnoutSnapshot>? TurnoutInfoReceived;

    public event EventHandler<StationHardware>? HardwareInfoReceived;

    public bool IsConnected => _station.IsConnected;

    public async Task ConnectAsync(string host, int port)
    {
        _options.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
        await _station.ConnectAsync();
        await RequestCurrentStateAsync();
    }

    public async Task<bool> ConfirmSessionAsync(CancellationToken token)
    {
        var echoed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnEcho(object? sender, BroadcastFlagsReceivedEventArgs args) => echoed.TrySetResult();

        _broadcastFlagsReadback.OnBroadcastFlagsReceived += OnEcho;
        await using var cancellation = token.Register(() => echoed.TrySetCanceled(token));
        try
        {
            await ArmBroadcastFlagsAsync();
            await _station.SendCommandsAsync(_station.Commands.Create<GetBroadcastFlagsCommand>(Array.Empty<object>()));
            await echoed.Task;
            await RequestFeedbackAndStateAsync();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _broadcastFlagsReadback.OnBroadcastFlagsReceived -= OnEcho;
        }
    }

    public async Task RequestCurrentStateAsync()
    {
        await ArmBroadcastFlagsAsync();
        await RequestFeedbackAndStateAsync();
    }

    private Task ArmBroadcastFlagsAsync() =>
        _station.SendCommandsAsync(_station.Commands.Create<SetBroadcastFlagsCommand>(new object[] { _broadcastFlags }));

    private async Task RequestFeedbackAndStateAsync()
    {
        await _station.RequestFeedbackAsync(0);
        await _station.RequestFeedbackAsync(1);
        await _station.RequestSystemStateAsync();
        await _station.SendCommandsAsync(_station.Commands.Create<GetHardwareInfoCommand>(Array.Empty<object>()));
    }

    public Task DisconnectAsync() => _station.DisconnectAsync();

    public Task SetTrackPowerAsync(bool on) =>
        on ? _station.TrackPowerOnAsync() : _station.TrackPowerOffAsync();

    private void OnFeedbackChanged(object? sender, FeedbackData data) =>
        FeedbackReceived?.Invoke(this, _decoder.Decode(data));

    private void OnTrackPowerChanged(object? sender, bool on) =>
        TrackPowerChanged?.Invoke(this, on);

    private void OnConnectionChanged(object? sender, ConnectionChangedEventArgs args) =>
        ConnectionChanged?.Invoke(this, args.IsConnected);

    private void OnSystemStateReceived(object? sender, SystemState state) =>
        SystemStateReceived?.Invoke(this, _mapper.ToSystem(state));

    private void OnLocoInfoReceived(object? sender, LocoInfoData loco) =>
        LocoInfoReceived?.Invoke(this, _mapper.ToLoco(loco));

    private void OnTurnoutInfoReceived(object? sender, TurnoutInfo turnout) =>
        TurnoutInfoReceived?.Invoke(this, _mapper.ToTurnout(turnout));

    private void OnHardwareInfoReceived(object? sender, HardwareInfoEventArgs hardware) =>
        HardwareInfoReceived?.Invoke(this, _mapper.ToHardware(hardware));
}
