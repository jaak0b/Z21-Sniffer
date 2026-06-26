using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class IntervalSourceBaseTest
{
    private sealed class TestInterval : IntervalBase
    {
    }

    private sealed class TestSource : IntervalSourceBase<TestInterval>
    {
        public TestInterval Open(DateTimeOffset start) => CreateInterval(start);
    }

    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private TestSource _source = null!;

    [SetUp]
    public void SetUp() => _source = new TestSource();

    [Test]
    public void CreateInterval_SetsStartOpenAndTracksCurrent()
    {
        var interval = _source.Open(T0);

        Assert.That(interval.Start, Is.EqualTo(T0));
        Assert.That(interval.IsOpen, Is.True);
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.Open));
        Assert.That(_source.CurrentInterval, Is.SameAs(interval));
        Assert.That(_source.Intervals, Is.EqualTo(new[] { interval }));
    }

    [Test]
    public void CreateInterval_AssignsDistinctKeys()
    {
        var first = _source.Open(T0);
        _source.CloseInterval(T0, IntervalEndReason.FallingEdge);
        var second = _source.Open(T0);

        Assert.That(first.Key, Is.Not.EqualTo(second.Key));
    }

    [Test]
    public void CloseInterval_SetsEndAndReasonInPlaceAndClearsCurrent()
    {
        var interval = _source.Open(T0);
        var at = T0 + TimeSpan.FromSeconds(4);

        _source.CloseInterval(at, IntervalEndReason.Stopped);

        Assert.That(interval.End, Is.EqualTo(at));
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.Stopped));
        Assert.That(_source.CurrentInterval, Is.Null);
    }

    [Test]
    public void CloseInterval_WithoutCurrent_DoesNothing()
    {
        _source.CloseInterval(T0, IntervalEndReason.FallingEdge);

        Assert.That(_source.Intervals, Is.Empty);
        Assert.That(_source.CurrentInterval, Is.Null);
    }

    [Test]
    public void CloseOpenIntervals_ClosesEveryOpenIntervalWithReason()
    {
        var interval = _source.Open(T0);
        var at = T0 + TimeSpan.FromSeconds(2);

        _source.CloseOpenIntervals(at, IntervalEndReason.Stopped);

        Assert.That(interval.End, Is.EqualTo(at));
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.Stopped));
        Assert.That(_source.CurrentInterval, Is.Null);
    }

    [Test]
    public void CloseOpenIntervals_LeavesAlreadyClosedIntervalsUntouched()
    {
        var first = _source.Open(T0);
        _source.CloseInterval(T0 + TimeSpan.FromSeconds(1), IntervalEndReason.FallingEdge);

        _source.CloseOpenIntervals(T0 + TimeSpan.FromSeconds(9), IntervalEndReason.Stopped);

        Assert.That(first.End, Is.EqualTo(T0 + TimeSpan.FromSeconds(1)));
        Assert.That(first.EndReason, Is.EqualTo(IntervalEndReason.FallingEdge));
    }

    [Test]
    public void Upsert_ReplacesIntervalWithSameKey()
    {
        var original = _source.Open(T0);
        var replacement = new TestInterval { Key = original.Key, Start = T0 + TimeSpan.FromSeconds(5) };

        _source.Upsert(replacement);

        Assert.That(_source.Intervals, Is.EqualTo(new[] { replacement }));
    }

    [Test]
    public void Upsert_AddsIntervalWithNewKey()
    {
        var added = new TestInterval { Key = "external", Start = T0 };

        _source.Upsert(added);

        Assert.That(_source.Intervals, Is.EqualTo(new[] { added }));
    }

    [Test]
    public void Clear_RemovesAllIntervalsAndCurrent()
    {
        _source.Open(T0);

        _source.Clear();

        Assert.That(_source.Intervals, Is.Empty);
        Assert.That(_source.CurrentInterval, Is.Null);
    }

    [Test]
    public void IntervalType_IsTheGenericArgument()
    {
        Assert.That(_source.IntervalType, Is.EqualTo(typeof(TestInterval)));
    }

    [Test]
    public void Order_WithNoPersistedValue_ReturnsSeed()
    {
        _source.Id = "s1";
        _source.SeedOrder(5);

        Assert.That(_source.Order, Is.EqualTo(5));
    }

    [Test]
    public void Order_Set_PersistsToBoundStoreKeyedById()
    {
        var store = new InMemoryKeyValueStore();
        _source.Id = "s1";
        _source.UsePersistence(store);
        _source.SeedOrder(5);

        _source.Order = 9;

        Assert.That(store.GetValue<int>("s1/order"), Is.EqualTo(9));
        Assert.That(_source.Order, Is.EqualTo(9));
    }

    [Test]
    public void Order_PersistedValueOverridesSeed()
    {
        var store = new InMemoryKeyValueStore();
        store.SetValue("s1/order", 3);
        _source.Id = "s1";
        _source.UsePersistence(store);
        _source.SeedOrder(99);

        Assert.That(_source.Order, Is.EqualTo(3));
    }

    [Test]
    public void CreateInterval_RaisesChanged()
    {
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.Open(T0);

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void CloseInterval_RaisesChanged()
    {
        _source.Open(T0);
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.CloseInterval(T0 + TimeSpan.FromSeconds(1), IntervalEndReason.FallingEdge);

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void CloseInterval_WithoutCurrent_DoesNotRaiseChanged()
    {
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.CloseInterval(T0, IntervalEndReason.FallingEdge);

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void CloseOpenIntervals_NothingOpen_DoesNotRaiseChanged()
    {
        _source.Open(T0);
        _source.CloseInterval(T0 + TimeSpan.FromSeconds(1), IntervalEndReason.FallingEdge);
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.CloseOpenIntervals(T0 + TimeSpan.FromSeconds(2), IntervalEndReason.Stopped);

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Upsert_RaisesChanged()
    {
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.Upsert(new TestInterval { Key = "k", Start = T0 });

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Clear_RaisesChanged()
    {
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.Clear();

        Assert.That(raised, Is.EqualTo(1));
    }
}
