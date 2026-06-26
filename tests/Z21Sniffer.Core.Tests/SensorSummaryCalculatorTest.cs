using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class SensorSummaryCalculatorTest
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 26, 10, 0, 0, TimeSpan.Zero);
    private readonly SensorSummaryCalculator _calculator = new();

    private static readonly SensorKey SensorA = new(1, 1);
    private static readonly SensorKey Ghost = new(2, 3);

    [Test]
    public void Summarize_NoIntervals_ReturnsEmpty()
    {
        Assert.That(_calculator.Summarize([], [], T0), Is.Empty);
    }

    [Test]
    public void Summarize_ClosedIntervals_ComputesCountTotalShortestLongest()
    {
        var intervals = new[]
        {
            new SensorInterval(SensorA, T0, T0.AddSeconds(2)),
            new SensorInterval(SensorA, T0.AddSeconds(10), T0.AddSeconds(15))
        };

        var summary = _calculator.Summarize(intervals, [], T0.AddSeconds(20)).Single();

        Assert.That(summary.OnCount, Is.EqualTo(2));
        Assert.That(summary.TotalOnSeconds, Is.EqualTo(7).Within(1e-9));
        Assert.That(summary.ShortestOnSeconds, Is.EqualTo(2).Within(1e-9));
        Assert.That(summary.LongestOnSeconds, Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void Summarize_OpenInterval_CountedToNow()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0, null) };

        var summary = _calculator.Summarize(intervals, [], T0.AddSeconds(8)).Single();

        Assert.That(summary.OnCount, Is.EqualTo(1));
        Assert.That(summary.TotalOnSeconds, Is.EqualTo(8).Within(1e-9));
        Assert.That(summary.LongestOnSeconds, Is.EqualTo(8).Within(1e-9));
    }

    [Test]
    public void Summarize_GhostFlap_HasTinyShortestOnTime()
    {
        var intervals = Enumerable.Range(0, 5)
            .Select(i => new SensorInterval(Ghost, T0.AddSeconds(i), T0.AddSeconds(i).AddMilliseconds(40)))
            .ToArray();

        var summary = _calculator.Summarize(intervals, [], T0.AddSeconds(20)).Single();

        Assert.That(summary.OnCount, Is.EqualTo(5));
        Assert.That(summary.ShortestOnSeconds, Is.EqualTo(0.04).Within(1e-9));
    }

    [Test]
    public void Summarize_UsesAliasLabel()
    {
        var intervals = new[] { new SensorInterval(SensorA, T0, T0.AddSeconds(1)) };
        var aliases = new[] { new SensorAlias(SensorA, "Station track 2") };

        Assert.That(_calculator.Summarize(intervals, aliases, T0.AddSeconds(2)).Single().Label,
            Is.EqualTo("Station track 2"));
    }

    [Test]
    public void Summarize_OrdersByModuleThenContact()
    {
        var intervals = new[]
        {
            new SensorInterval(new SensorKey(2, 1), T0, T0.AddSeconds(1)),
            new SensorInterval(new SensorKey(1, 5), T0, T0.AddSeconds(1)),
            new SensorInterval(new SensorKey(1, 2), T0, T0.AddSeconds(1))
        };

        var ordered = _calculator.Summarize(intervals, [], T0.AddSeconds(2))
            .Select(s => (s.Module, s.Contact)).ToArray();

        Assert.That(ordered, Is.EqualTo(new[] { (1, 2), (1, 5), (2, 1) }));
    }
}
