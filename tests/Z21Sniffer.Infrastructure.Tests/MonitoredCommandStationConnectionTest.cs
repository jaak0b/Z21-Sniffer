using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Infrastructure;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class MonitoredCommandStationConnectionTest
{
    private ICommandStationConnection _inner = null!;
    private INetworkReachability _reachability = null!;
    private MonitoredCommandStationConnection _monitored = null!;

    [SetUp]
    public void SetUp()
    {
        _inner = A.Fake<ICommandStationConnection>();
        _reachability = A.Fake<INetworkReachability>();
        _monitored = new MonitoredCommandStationConnection(_inner, _reachability, A.Fake<ILogger<MonitoredCommandStationConnection>>());
    }

    [TearDown]
    public void TearDown() => _monitored.Dispose();

    private void Reachable(bool alive) =>
        A.CallTo(() => _reachability.IsReachableAsync(A<string>._, A<TimeSpan>._, A<CancellationToken>._)).Returns(alive);

    private void SessionConfirms(bool confirmed) =>
        A.CallTo(() => _inner.ConfirmSessionAsync(A<CancellationToken>._)).Returns(confirmed);

    [Test]
    public async Task ConnectAsync_OpensInnerConnection()
    {
        await _monitored.ConnectAsync("10.0.0.5", 21105);

        A.CallTo(() => _inner.ConnectAsync("10.0.0.5", 21105)).MustHaveHappened();
    }

    [Test]
    public async Task Poll_PingsTheConnectHostWithTheConfiguredTimeout()
    {
        await _monitored.ConnectAsync("10.0.0.5", 21105);

        await _monitored.PollAsync();

        A.CallTo(() => _reachability.IsReachableAsync("10.0.0.5", TimeSpan.FromSeconds(1), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Test]
    public async Task Poll_WhenUnreachable_NeitherConfirmsNorConnects()
    {
        bool? connected = null;
        _monitored.ConnectionChanged += (_, c) => connected = c;
        Reachable(false);

        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();

        A.CallTo(() => _inner.ConfirmSessionAsync(A<CancellationToken>._)).MustNotHaveHappened();
        Assert.That(connected, Is.Null);
        Assert.That(_monitored.IsConnected, Is.False);
    }

    [Test]
    public async Task Poll_WhenReachableAndSessionConfirms_ReportsConnectedOnce()
    {
        var events = new List<bool>();
        _monitored.ConnectionChanged += (_, c) => events.Add(c);
        Reachable(true);
        SessionConfirms(true);

        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();

        Assert.That(events, Is.EqualTo(new[] { true }));
        Assert.That(_monitored.IsConnected, Is.True);
        A.CallTo(() => _inner.ConfirmSessionAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Poll_WhenReachableButConfirmationFails_StaysDisconnected()
    {
        bool? connected = null;
        _monitored.ConnectionChanged += (_, c) => connected = c;
        Reachable(true);
        SessionConfirms(false);

        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();

        Assert.That(connected, Is.Null);
        Assert.That(_monitored.IsConnected, Is.False);
    }

    [Test]
    public async Task Poll_WhenConfirmationThrows_StaysDisconnectedThenRetries()
    {
        A.CallTo(() => _inner.ConfirmSessionAsync(A<CancellationToken>._))
            .Throws<InvalidOperationException>().Once()
            .Then.Returns(true);
        Reachable(true);
        await _monitored.ConnectAsync("10.0.0.5", 21105);

        await _monitored.PollAsync();
        Assert.That(_monitored.IsConnected, Is.False);

        await _monitored.PollAsync();
        Assert.That(_monitored.IsConnected, Is.True);
    }

    [Test]
    public async Task Poll_OnceConfirmed_DoesNotConfirmAgain()
    {
        Reachable(true);
        SessionConfirms(true);

        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();
        await _monitored.PollAsync();

        A.CallTo(() => _inner.ConfirmSessionAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Poll_WhenPingDropsDuringConfirmation_CancelsTheInFlightConfirmation()
    {
        CancellationToken captured = default;
        var pending = new TaskCompletionSource<bool>();
        A.CallTo(() => _inner.ConfirmSessionAsync(A<CancellationToken>._))
            .Invokes((CancellationToken token) => captured = token)
            .Returns(pending.Task);
        Reachable(true);
        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();

        Reachable(false);
        await _monitored.PollAsync();

        Assert.That(captured.IsCancellationRequested, Is.True);
    }

    [Test]
    public async Task Poll_PingDiesAfterConnecting_ReportsDisconnected()
    {
        var events = new List<bool>();
        _monitored.ConnectionChanged += (_, c) => events.Add(c);
        Reachable(true);
        SessionConfirms(true);
        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();

        Reachable(false);
        await _monitored.PollAsync();

        Assert.That(events, Is.EqualTo(new[] { true, false }));
        Assert.That(_monitored.IsConnected, Is.False);
    }

    [Test]
    public async Task Poll_PingRecovers_ReconnectsAndConfirmsAgain()
    {
        var events = new List<bool>();
        _monitored.ConnectionChanged += (_, c) => events.Add(c);
        Reachable(true);
        SessionConfirms(true);
        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();
        Reachable(false);
        await _monitored.PollAsync();

        Reachable(true);
        await _monitored.PollAsync();

        Assert.That(events, Is.EqualTo(new[] { true, false, true }));
        A.CallTo(() => _inner.ConfirmSessionAsync(A<CancellationToken>._)).MustHaveHappenedTwiceExactly();
    }

    [Test]
    public async Task Poll_WhileConnected_RefreshesCurrentStateOnlyAfterTheConfiguredPollCount()
    {
        Reachable(true);
        SessionConfirms(true);
        await _monitored.ConnectAsync("10.0.0.5", 21105);

        await _monitored.PollAsync();
        await _monitored.PollAsync();
        await _monitored.PollAsync();
        A.CallTo(() => _inner.RequestCurrentStateAsync()).MustNotHaveHappened();

        await _monitored.PollAsync();
        A.CallTo(() => _inner.RequestCurrentStateAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Poll_WhileDisconnected_NeverRefreshesCurrentState()
    {
        Reachable(false);
        await _monitored.ConnectAsync("10.0.0.5", 21105);

        for (var i = 0; i < 6; i++) await _monitored.PollAsync();

        A.CallTo(() => _inner.RequestCurrentStateAsync()).MustNotHaveHappened();
    }

    [Test]
    public void FeedbackReceived_ForwardsInnerEvent()
    {
        IReadOnlyList<SensorState>? received = null;
        _monitored.FeedbackReceived += (_, states) => received = states;

        _inner.FeedbackReceived += Raise.With<IReadOnlyList<SensorState>>(_inner, new[] { new SensorState(new SensorKey(1, 1), true) });

        Assert.That(received, Is.Not.Null);
        Assert.That(received, Is.Not.Empty);
    }

    [Test]
    public void SystemStateReceived_ForwardsInnerEvent()
    {
        SystemSnapshot? snapshot = null;
        _monitored.SystemStateReceived += (_, s) => snapshot = s;

        _inner.SystemStateReceived += Raise.With(_inner, A.Dummy<SystemSnapshot>());

        Assert.That(snapshot, Is.Not.Null);
    }

    [Test]
    public async Task RequestCurrentStateAsync_ForwardsToInner()
    {
        await _monitored.RequestCurrentStateAsync();

        A.CallTo(() => _inner.RequestCurrentStateAsync()).MustHaveHappened();
    }

    [Test]
    public async Task SetTrackPowerAsync_ForwardsToInner()
    {
        await _monitored.SetTrackPowerAsync(true);

        A.CallTo(() => _inner.SetTrackPowerAsync(true)).MustHaveHappened();
    }

    [Test]
    public async Task DisconnectAsync_DisconnectsInnerAndGoesDisconnected()
    {
        Reachable(true);
        SessionConfirms(true);
        await _monitored.ConnectAsync("10.0.0.5", 21105);
        await _monitored.PollAsync();

        await _monitored.DisconnectAsync();

        A.CallTo(() => _inner.DisconnectAsync()).MustHaveHappened();
        Assert.That(_monitored.IsConnected, Is.False);
    }
}
