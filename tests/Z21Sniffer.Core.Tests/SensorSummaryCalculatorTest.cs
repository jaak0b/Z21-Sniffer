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

    private static FeedbackSensorSource Source(SensorKey sensor, string? label, params (DateTimeOffset Start, DateTimeOffset? End)[] intervals)
    {
        var source = new FeedbackSensorSource { Id = $"sensor:{sensor.Module}.{sensor.Contact}", Sensor = sensor };
        if (label is not null) source.Label = label;
        foreach (var (start, end) in intervals)
        {
            source.Apply(occupied: true, start);
            if (end is { } e) source.Apply(occupied: false, e);
        }

        return source;
    }

    [Test]
    public void Summarize_NoSources_ReturnsEmpty()
    {
        Assert.That(_calculator.Summarize([], T0), Is.Empty);
    }

    [Test]
    public void Summarize_ClosedIntervals_ComputesCountTotalShortestLongest()
    {
        var source = Source(SensorA, null, (T0, T0.AddSeconds(2)), (T0.AddSeconds(10), T0.AddSeconds(15)));

        var summary = _calculator.Summarize([source], T0.AddSeconds(20)).Single();

        Assert.That(summary.OnCount, Is.EqualTo(2));
        Assert.That(summary.TotalOnSeconds, Is.EqualTo(7).Within(1e-9));
        Assert.That(summary.ShortestOnSeconds, Is.EqualTo(2).Within(1e-9));
        Assert.That(summary.LongestOnSeconds, Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void Summarize_OpenInterval_CountedToNow()
    {
        var source = Source(SensorA, null, (T0, null));

        var summary = _calculator.Summarize([source], T0.AddSeconds(8)).Single();

        Assert.That(summary.OnCount, Is.EqualTo(1));
        Assert.That(summary.TotalOnSeconds, Is.EqualTo(8).Within(1e-9));
        Assert.That(summary.LongestOnSeconds, Is.EqualTo(8).Within(1e-9));
    }

    [Test]
    public void Summarize_GhostFlap_HasTinyShortestOnTime()
    {
        var flaps = Enumerable.Range(0, 5)
            .Select(i => (T0.AddSeconds(i), (DateTimeOffset?)T0.AddSeconds(i).AddMilliseconds(40)))
            .ToArray();
        var source = Source(Ghost, null, flaps);

        var summary = _calculator.Summarize([source], T0.AddSeconds(20)).Single();

        Assert.That(summary.OnCount, Is.EqualTo(5));
        Assert.That(summary.ShortestOnSeconds, Is.EqualTo(0.04).Within(1e-9));
    }

    [Test]
    public void Summarize_UsesSourceLabel()
    {
        var source = Source(SensorA, "Station track 2", (T0, T0.AddSeconds(1)));

        Assert.That(_calculator.Summarize([source], T0.AddSeconds(2)).Single().Label, Is.EqualTo("Station track 2"));
    }

    [Test]
    public void Summarize_OrdersByModuleThenContact()
    {
        var sources = new[]
        {
            Source(new SensorKey(2, 1), null, (T0, T0.AddSeconds(1))),
            Source(new SensorKey(1, 5), null, (T0, T0.AddSeconds(1))),
            Source(new SensorKey(1, 2), null, (T0, T0.AddSeconds(1))),
        };

        var ordered = _calculator.Summarize(sources, T0.AddSeconds(2)).Select(s => (s.Module, s.Contact)).ToArray();

        Assert.That(ordered, Is.EqualTo(new[] { (1, 2), (1, 5), (2, 1) }));
    }

    [Test]
    public void Summarize_SourceWithoutIntervals_IsSkipped()
    {
        var source = new FeedbackSensorSource { Id = "sensor:1.1", Sensor = SensorA };

        Assert.That(_calculator.Summarize([source], T0), Is.Empty);
    }
}
