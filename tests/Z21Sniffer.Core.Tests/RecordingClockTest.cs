using NUnit.Framework;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class RecordingClockTest
{
    private FakeClock _inner = null!;
    private RecordingClock _clock = null!;

    [SetUp]
    public void SetUp()
    {
        _inner = new FakeClock();
        _clock = new RecordingClock(_inner);
    }

    [Test]
    public void Now_BeforeStart_IsFrozen()
    {
        var before = _clock.Now;
        _inner.Advance(TimeSpan.FromSeconds(10));

        Assert.That(_clock.Now, Is.EqualTo(before));
        Assert.That(_clock.IsRunning, Is.False);
    }

    [Test]
    public void Now_WhileRunning_TracksInnerClock()
    {
        _clock.Start();
        _inner.Advance(TimeSpan.FromSeconds(7));

        Assert.That(_clock.Now, Is.EqualTo(_inner.Now));
        Assert.That(_clock.IsRunning, Is.True);
    }

    [Test]
    public void Now_AfterStop_IsFrozenAtStopInstant()
    {
        _clock.Start();
        _inner.Advance(TimeSpan.FromSeconds(5));
        var atStop = _inner.Now;
        _clock.Stop();
        _inner.Advance(TimeSpan.FromSeconds(9));

        Assert.That(_clock.Now, Is.EqualTo(atStop));
        Assert.That(_clock.IsRunning, Is.False);
    }

    [Test]
    public void Start_AfterStop_ResumesTrackingInnerClock()
    {
        _clock.Start();
        _inner.Advance(TimeSpan.FromSeconds(5));
        _clock.Stop();
        _inner.Advance(TimeSpan.FromSeconds(9));

        _clock.Start();

        Assert.That(_clock.Now, Is.EqualTo(_inner.Now));
    }

    [Test]
    public void Start_RaisesRunningChanged()
    {
        var raised = 0;
        _clock.RunningChanged += (_, _) => raised++;

        _clock.Start();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Start_WhenAlreadyRunning_DoesNotRaiseRunningChanged()
    {
        _clock.Start();
        var raised = 0;
        _clock.RunningChanged += (_, _) => raised++;

        _clock.Start();

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Stop_RaisesRunningChanged()
    {
        _clock.Start();
        var raised = 0;
        _clock.RunningChanged += (_, _) => raised++;

        _clock.Stop();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Stop_WhenNotRunning_DoesNotRaiseRunningChanged()
    {
        var raised = 0;
        _clock.RunningChanged += (_, _) => raised++;

        _clock.Stop();

        Assert.That(raised, Is.EqualTo(0));
    }
}
