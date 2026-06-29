using NUnit.Framework;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class IntervalSourceOrderRegistryTest
{
    private InMemoryKeyValueStore _store = null!;
    private IntervalSourceOrderRegistry _order = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryKeyValueStore();
        _order = new IntervalSourceOrderRegistry(_store);
    }

    [Test]
    public void IndexOf_UnregisteredId_IsMinusOne()
    {
        Assert.That(_order.IndexOf("a"), Is.EqualTo(-1));
    }

    [Test]
    public void Register_AppendsIdsAtTheBottom()
    {
        _order.Register("a");
        _order.Register("b");
        _order.Register("c");

        Assert.That(_order.IndexOf("a"), Is.EqualTo(0));
        Assert.That(_order.IndexOf("b"), Is.EqualTo(1));
        Assert.That(_order.IndexOf("c"), Is.EqualTo(2));
    }

    [Test]
    public void Clear_ForgetsEveryId()
    {
        _order.Register("a");
        _order.Register("b");

        _order.Clear();

        Assert.That(_order.IndexOf("a"), Is.EqualTo(-1));
        Assert.That(_order.IndexOf("b"), Is.EqualTo(-1));
    }

    [Test]
    public void Clear_IsPersistedForLaterRuns()
    {
        _order.Register("a");
        _order.Clear();

        var nextRun = new IntervalSourceOrderRegistry(_store);
        Assert.That(nextRun.IndexOf("a"), Is.EqualTo(-1));
    }

    [Test]
    public void Register_KnownId_DoesNotMoveOrDuplicateIt()
    {
        _order.Register("a");
        _order.Register("a");
        _order.Register("b");

        Assert.That(_order.IndexOf("a"), Is.EqualTo(0));
        Assert.That(_order.IndexOf("b"), Is.EqualTo(1), "a re-registered id must not be duplicated");
    }

    [Test]
    public void Register_PersistsThroughTheStore()
    {
        _order.Register("a");
        _order.Register("b");

        var reloaded = new IntervalSourceOrderRegistry(_store);

        Assert.That(reloaded.IndexOf("a"), Is.EqualTo(0));
        Assert.That(reloaded.IndexOf("b"), Is.EqualTo(1));
    }

    [Test]
    public void Insert_AfterAKnownId_PlacesItRightAfterThatId()
    {
        _order.Register("a");
        _order.Register("b");
        _order.Register("c");

        _order.Insert("x", "a");

        Assert.That(_order.IndexOf("a"), Is.EqualTo(0));
        Assert.That(_order.IndexOf("x"), Is.EqualTo(1));
        Assert.That(_order.IndexOf("b"), Is.EqualTo(2));
        Assert.That(_order.IndexOf("c"), Is.EqualTo(3));
    }

    [Test]
    public void Insert_WithNullAfterId_AppendsAtTheBottom()
    {
        _order.Register("a");

        _order.Insert("x", null);

        Assert.That(_order.IndexOf("x"), Is.EqualTo(1));
    }

    [Test]
    public void Insert_WithUnknownAfterId_AppendsAtTheBottom()
    {
        _order.Register("a");

        _order.Insert("x", "missing");

        Assert.That(_order.IndexOf("x"), Is.EqualTo(1));
    }

    [Test]
    public void Insert_KnownId_DoesNotMoveOrDuplicateIt()
    {
        _order.Register("a");
        _order.Register("b");

        _order.Insert("a", "b");

        Assert.That(_order.IndexOf("a"), Is.EqualTo(0));
        Assert.That(_order.IndexOf("b"), Is.EqualTo(1));
    }

    [Test]
    public void Insert_Persists()
    {
        _order.Register("a");
        _order.Register("b");
        _order.Insert("x", "a");

        var reloaded = new IntervalSourceOrderRegistry(_store);

        Assert.That(reloaded.IndexOf("x"), Is.EqualTo(1));
    }

    [Test]
    public void Reorder_RepositionsTheGivenIds()
    {
        _order.Register("a");
        _order.Register("b");
        _order.Register("c");

        _order.Reorder(new[] { "c", "a", "b" });

        Assert.That(_order.IndexOf("c"), Is.EqualTo(0));
        Assert.That(_order.IndexOf("a"), Is.EqualTo(1));
        Assert.That(_order.IndexOf("b"), Is.EqualTo(2));
    }

    [Test]
    public void Reorder_KeepsIdsOutsideTheSubsetAnchoredToTheirSlots()
    {
        _order.Register("a");
        _order.Register("hidden");
        _order.Register("b");
        _order.Register("c");

        _order.Reorder(new[] { "c", "b", "a" });

        Assert.That(_order.IndexOf("c"), Is.EqualTo(0));
        Assert.That(_order.IndexOf("hidden"), Is.EqualTo(1));
        Assert.That(_order.IndexOf("b"), Is.EqualTo(2));
        Assert.That(_order.IndexOf("a"), Is.EqualTo(3));
    }

    [Test]
    public void Reorder_AppendsUnknownIdsAtTheBottom()
    {
        _order.Register("a");

        _order.Reorder(new[] { "a", "z" });

        Assert.That(_order.IndexOf("a"), Is.EqualTo(0));
        Assert.That(_order.IndexOf("z"), Is.EqualTo(1));
    }

    [Test]
    public void Reorder_Persists()
    {
        _order.Register("a");
        _order.Register("b");
        _order.Reorder(new[] { "b", "a" });

        var reloaded = new IntervalSourceOrderRegistry(_store);

        Assert.That(reloaded.IndexOf("b"), Is.EqualTo(0));
        Assert.That(reloaded.IndexOf("a"), Is.EqualTo(1));
    }

    [Test]
    public void Register_AbsentRememberedId_KeepsItsSlotWhenNewIdsArriveFirst()
    {
        _order.Register("a");
        _order.Register("b");
        _order.Register("c");
        _order.Reorder(new[] { "b", "a", "c" });

        var nextRun = new IntervalSourceOrderRegistry(_store);
        nextRun.Register("z");
        nextRun.Register("a");

        Assert.That(nextRun.IndexOf("a"), Is.EqualTo(1));
        Assert.That(nextRun.IndexOf("z"), Is.GreaterThan(nextRun.IndexOf("a")));
    }
}
