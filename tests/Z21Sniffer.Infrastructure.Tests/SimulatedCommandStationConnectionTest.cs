using FakeItEasy;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Infrastructure.Simulation;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class SimulatedCommandStationConnectionTest
{
    private SimulatedCommandStationConnection _connection = null!;

    [SetUp]
    public void SetUp() => _connection = new SimulatedCommandStationConnection(new SimulatedFeedbackScript());

    [TearDown]
    public void TearDown() => _connection.Dispose();

    [Test]
    public async Task ConnectAsync_MarksConnectedAndAnnouncesPower()
    {
        bool? connected = null;
        bool? power = null;
        _connection.ConnectionChanged += (_, c) => connected = c;
        _connection.TrackPowerChanged += (_, p) => power = p;

        await _connection.ConnectAsync("ignored", 0);

        Assert.That(_connection.IsConnected, Is.True);
        Assert.That(connected, Is.True);
        Assert.That(power, Is.True);
    }

    [Test]
    public void EmitNext_RaisesFeedbackReceived()
    {
        IReadOnlyList<SensorState>? received = null;
        _connection.FeedbackReceived += (_, states) => received = states;

        _connection.EmitNext();

        Assert.That(received, Is.Not.Null);
        Assert.That(received, Is.Not.Empty);
    }

    [Test]
    public void EmitNext_OnFifthTick_RaisesSystemState()
    {
        SystemSnapshot? snapshot = null;
        _connection.SystemStateReceived += (_, s) => snapshot = s;

        for (var i = 0; i < 5; i++) _connection.EmitNext();

        Assert.That(snapshot, Is.Not.Null);
    }

    [Test]
    public async Task ConnectAsync_SeedsSystemState()
    {
        SystemSnapshot? snapshot = null;
        _connection.SystemStateReceived += (_, s) => snapshot = s;

        await _connection.ConnectAsync("ignored", 0);

        Assert.That(snapshot, Is.Not.Null);
    }

    [Test]
    public async Task ConnectAsync_CalledTwice_StaysConnectedAndDisconnectsCleanly()
    {
        await _connection.ConnectAsync("ignored", 0);
        await _connection.ConnectAsync("ignored", 0);
        Assert.That(_connection.IsConnected, Is.True);

        await _connection.DisconnectAsync();

        Assert.That(_connection.IsConnected, Is.False);
    }

    [Test]
    public async Task SetTrackPowerAsync_RaisesTrackPowerChanged()
    {
        bool? power = null;
        _connection.TrackPowerChanged += (_, on) => power = on;

        await _connection.SetTrackPowerAsync(false);

        Assert.That(power, Is.False);
    }

    [Test]
    public async Task DisconnectAsync_MarksDisconnected()
    {
        await _connection.ConnectAsync("ignored", 0);

        await _connection.DisconnectAsync();

        Assert.That(_connection.IsConnected, Is.False);
    }
}
