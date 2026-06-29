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
    public void HighlightsShortIntervals_DefaultsToTrue() =>
        Assert.That(_source.HighlightsShortIntervals, Is.True);

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
    public void CreateInterval_AssignsSequentiallyIncreasingKeys()
    {
        var first = _source.Open(T0);
        _source.CloseInterval(T0, IntervalEndReason.FallingEdge);
        var second = _source.Open(T0);

        Assert.That(first.Key, Is.EqualTo("0"));
        Assert.That(second.Key, Is.EqualTo("1"));
    }

    [Test]
    public void Id_DefaultsToEmptyString()
    {
        Assert.That(new TestSource().Id, Is.Empty);
    }

    [Test]
    public void Intervals_SetWithAList_KeepsTheSameInstance()
    {
        var list = new List<TestInterval> { new() { Key = "k" } };

        _source.Intervals = list;

        Assert.That(_source.Intervals, Is.SameAs(list));
    }

    [Test]
    public void Intervals_SetWithANonList_CopiesTheItems()
    {
        var array = new TestInterval[] { new() { Key = "k" } };

        _source.Intervals = array;

        Assert.That(_source.Intervals, Is.Not.SameAs(array));
        Assert.That(_source.Intervals.Select(interval => interval.Key), Is.EqualTo(new[] { "k" }));
    }

    [Test]
    public void CloseOpenIntervals_WhenSomethingWasOpen_ClosesItAndRaisesChanged()
    {
        var interval = _source.Open(T0);
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.CloseOpenIntervals(T0.AddSeconds(3), IntervalEndReason.Stopped);

        Assert.That(interval.IsOpen, Is.False);
        Assert.That(interval.EndReason, Is.EqualTo(IntervalEndReason.Stopped));
        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void CloseOpenIntervals_WhenNothingWasOpen_DoesNotRaiseChanged()
    {
        _source.Open(T0);
        _source.CloseInterval(T0.AddSeconds(1), IntervalEndReason.FallingEdge);
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.CloseOpenIntervals(T0.AddSeconds(3), IntervalEndReason.Stopped);

        Assert.That(raised, Is.EqualTo(0));
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
    public void IsVisible_DefaultsToTrue()
    {
        Assert.That(_source.IsVisible, Is.True);
    }

    [Test]
    public void IsVisible_IsNotPersistedToTheBoundStore()
    {
        var store = new InMemoryKeyValueStore();
        _source.Id = "s1";
        _source.UsePersistence(store);

        _source.IsVisible = false;

        Assert.That(store.GetValue<bool?>("s1/visible"), Is.Null);
    }

    [Test]
    public void IsVisible_Changed_RaisesChanged()
    {
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.IsVisible = false;

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void IsVisible_SetToSameValue_DoesNotRaiseChanged()
    {
        var raised = 0;
        _source.Changed += (_, _) => raised++;

        _source.IsVisible = true;

        Assert.That(raised, Is.EqualTo(0));
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
