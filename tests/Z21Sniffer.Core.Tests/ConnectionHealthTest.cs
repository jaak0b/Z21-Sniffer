using NUnit.Framework;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class ConnectionHealthTest
{
    private ConnectionHealth _health = null!;

    [SetUp]
    public void SetUp() => _health = new ConnectionHealth();

    [Test]
    public void Update_ReachableAndConfirmedFromCold_RaisesConnected()
    {
        var transition = _health.Update(reachable: true, sessionConfirmed: true);

        Assert.That(transition.NowConnected, Is.True);
        Assert.That(transition.RaiseConnected, Is.True);
        Assert.That(transition.RaiseDisconnected, Is.False);
        Assert.That(_health.Composite, Is.True);
    }

    [Test]
    public void Update_ReachableButNotConfirmed_StaysDisconnected()
    {
        var transition = _health.Update(reachable: true, sessionConfirmed: false);

        Assert.That(transition.NowConnected, Is.False);
        Assert.That(transition.RaiseConnected, Is.False);
        Assert.That(transition.RaiseDisconnected, Is.False);
        Assert.That(_health.Composite, Is.False);
    }

    [Test]
    public void Update_ConfirmedButNotReachable_StaysDisconnected()
    {
        var transition = _health.Update(reachable: false, sessionConfirmed: true);

        Assert.That(transition.NowConnected, Is.False);
        Assert.That(_health.Composite, Is.False);
    }

    [Test]
    public void Update_AlreadyConnectedAndStillHealthy_RaisesNothing()
    {
        _health.Update(true, true);

        var transition = _health.Update(true, true);

        Assert.That(transition.NowConnected, Is.True);
        Assert.That(transition.RaiseConnected, Is.False);
        Assert.That(transition.RaiseDisconnected, Is.False);
    }

    [Test]
    public void Update_BecomesUnreachableWhileConnected_RaisesDisconnected()
    {
        _health.Update(true, true);

        var transition = _health.Update(false, true);

        Assert.That(transition.NowConnected, Is.False);
        Assert.That(transition.RaiseDisconnected, Is.True);
        Assert.That(transition.RaiseConnected, Is.False);
    }

    [Test]
    public void Update_StillDisconnected_RaisesNothing()
    {
        var transition = _health.Update(false, false);

        Assert.That(transition.RaiseDisconnected, Is.False);
        Assert.That(transition.RaiseConnected, Is.False);
    }

    [Test]
    public void Update_RecoversAfterDropping_RaisesConnectedAgain()
    {
        _health.Update(true, true);
        _health.Update(false, false);

        var transition = _health.Update(true, true);

        Assert.That(transition.RaiseConnected, Is.True);
        Assert.That(transition.RaiseDisconnected, Is.False);
    }
}
