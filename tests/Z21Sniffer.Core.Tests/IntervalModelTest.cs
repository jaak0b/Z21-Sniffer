using NUnit.Framework;
using Z21Sniffer.Core.Model;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class IntervalModelTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    [Test]
    public void FeedbackSensorInterval_OpenByDefault()
    {
        var interval = new FeedbackSensorInterval { Start = T0 };

        Assert.That(interval.IsOpen, Is.True);
        Assert.That(interval.Duration, Is.Null);
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.Open));
    }

    [Test]
    public void FeedbackSensorInterval_ClosedReportsDuration()
    {
        var interval = new FeedbackSensorInterval { Start = T0, End = T0 + TimeSpan.FromSeconds(2) };

        Assert.That(interval.IsOpen, Is.False);
        Assert.That(interval.Duration, Is.EqualTo(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void ConnectionInterval_CarriesConnectedFlag()
    {
        var interval = new ConnectionInterval { Start = T0, Connected = true };

        Assert.That(interval.Connected, Is.True);
        Assert.That(interval.IsOpen, Is.True);
    }
}
