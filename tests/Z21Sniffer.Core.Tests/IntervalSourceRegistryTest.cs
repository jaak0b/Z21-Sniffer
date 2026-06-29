using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;

namespace Z21Sniffer.Core.Tests;

[TestFixture]
public class IntervalSourceRegistryTest
{
    private InMemoryKeyValueStore _store = null!;
    private IntervalSourceRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryKeyValueStore();
        _registry = new IntervalSourceRegistry(_store);
    }

    [Test]
    public void GetOrCreate_NewKey_CreatesAndAddsWithThatId()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(source.Id, Is.EqualTo("sensor:1"));
        Assert.That(_registry.Sources, Is.EqualTo(new[] { source }));
    }

    [Test]
    public void GetOrCreate_ExistingKey_ReturnsTheSameInstance()
    {
        var first = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var second = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(second, Is.SameAs(first));
        Assert.That(_registry.Sources, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetOrCreate_KeepsCreationOrder()
    {
        var first = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var second = _registry.GetOrCreate<ConnectionSource>("connection");

        Assert.That(_registry.Sources, Is.EqualTo(new IIntervalSource[] { first, second }));
    }

    [Test]
    public void Reorder_RepositionsSourcesByTheGivenIds()
    {
        var first = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var second = _registry.GetOrCreate<FeedbackSensorSource>("sensor:2");

        _registry.Reorder(new[] { "sensor:2", "sensor:1" });

        Assert.That(_registry.Sources, Is.EqualTo(new IIntervalSource[] { second, first }));
    }

    [Test]
    public void Sources_HonourRememberedOrderRegardlessOfArrivalTime()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:2");
        _registry.Reorder(new[] { "sensor:2", "sensor:1" });

        var nextRun = new IntervalSourceRegistry(_store);
        var late = nextRun.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var early = nextRun.GetOrCreate<FeedbackSensorSource>("sensor:2");

        Assert.That(nextRun.Sources, Is.EqualTo(new IIntervalSource[] { early, late }));
    }

    [Test]
    public void GetOrCreate_NewSourceOfExistingType_InsertsAfterTheLastOfThatType()
    {
        _registry.GetOrCreate<ConnectionSource>("connection");
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        _registry.GetOrCreate<TrackPowerSource>("trackpower");

        _registry.GetOrCreate<FeedbackSensorSource>("sensor:2");

        Assert.That(_registry.Sources.Select(source => source.Id),
            Is.EqualTo(new[] { "connection", "sensor:1", "sensor:2", "trackpower" }));
    }

    [Test]
    public void GetOrCreate_FirstSourceOfAType_AppendsAtTheBottom()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        _registry.GetOrCreate<ConnectionSource>("connection");

        Assert.That(_registry.Sources.Select(source => source.Id),
            Is.EqualTo(new[] { "sensor:1", "connection" }));
    }

    [Test]
    public void GetOrCreate_AfterReorder_NewSourceLandsAtTheBottom()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:2");
        _registry.Reorder(new[] { "sensor:2", "sensor:1" });

        var late = _registry.GetOrCreate<FeedbackSensorSource>("sensor:3");

        Assert.That(_registry.Sources.Last(), Is.SameAs(late));
        Assert.That(_registry.Sources.Select(source => source.Id), Is.EqualTo(new[] { "sensor:2", "sensor:1", "sensor:3" }));
    }

    [Test]
    public void ResetOrder_GroupsByTypeThenOrdersByFirstIntervalStart()
    {
        var t0 = DateTimeOffset.UnixEpoch;
        var sensorB = _registry.GetOrCreate<FeedbackSensorSource>("sensor:b");
        var connection = _registry.GetOrCreate<ConnectionSource>("connection");
        var sensorA = _registry.GetOrCreate<FeedbackSensorSource>("sensor:a");
        sensorB.Apply(occupied: true, t0 + TimeSpan.FromSeconds(30));
        connection.Set(connected: true, t0 + TimeSpan.FromSeconds(10));
        sensorA.Apply(occupied: true, t0 + TimeSpan.FromSeconds(20));
        _registry.Reorder(new[] { "sensor:a", "connection", "sensor:b" });

        _registry.ResetOrder();

        Assert.That(_registry.Sources.Select(source => source.Id),
            Is.EqualTo(new[] { "connection", "sensor:a", "sensor:b" }));
    }

    [Test]
    public void ResetOrder_PlacesSourcesWithoutIntervalsLast()
    {
        var t0 = DateTimeOffset.UnixEpoch;
        var quiet = _registry.GetOrCreate<FeedbackSensorSource>("sensor:quiet");
        var active = _registry.GetOrCreate<FeedbackSensorSource>("sensor:active");
        active.Apply(occupied: true, t0 + TimeSpan.FromSeconds(5));
        _registry.Reorder(new[] { "sensor:quiet", "sensor:active" });

        _registry.ResetOrder();

        Assert.That(_registry.Sources.Select(source => source.Id),
            Is.EqualTo(new[] { "sensor:active", "sensor:quiet" }));
    }

    [Test]
    public void ResetOrder_IsPersistedForLaterRuns()
    {
        var t0 = DateTimeOffset.UnixEpoch;
        var sensorB = _registry.GetOrCreate<FeedbackSensorSource>("sensor:b");
        var sensorA = _registry.GetOrCreate<FeedbackSensorSource>("sensor:a");
        sensorB.Apply(occupied: true, t0 + TimeSpan.FromSeconds(20));
        sensorA.Apply(occupied: true, t0 + TimeSpan.FromSeconds(10));

        _registry.ResetOrder();

        var nextRun = new IntervalSourceRegistry(_store);
        nextRun.GetOrCreate<FeedbackSensorSource>("sensor:b");
        nextRun.GetOrCreate<FeedbackSensorSource>("sensor:a");
        Assert.That(nextRun.Sources.Select(source => source.Id), Is.EqualTo(new[] { "sensor:a", "sensor:b" }));
    }

    [Test]
    public void ResetOrder_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.ResetOrder();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void ResetAliases_RestoresDefaultLabelOnAPresentSource()
    {
        var sensor = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1.1", s => s.Sensor = new SensorKey(1, 1));
        sensor.Label = "Yard 3";

        _registry.ResetAliases();

        Assert.That(sensor.Label, Is.EqualTo("M1.1"));
    }

    [Test]
    public void ResetAliases_ClearsStoredAliasesForAbsentSourcesToo()
    {
        _store.SetValue("sensor:9.9/label", "Hidden yard");

        _registry.ResetAliases();

        Assert.That(_store.GetValue<string>("sensor:9.9/label"), Is.Null);
    }

    [Test]
    public void ResetAliases_LeavesNonAliasKeysUntouched()
    {
        _store.SetValue("source-order", new List<string> { "sensor:1" });

        _registry.ResetAliases();

        Assert.That(_store.GetValue<List<string>>("source-order"), Is.Not.Null);
    }

    [Test]
    public void ResetAliases_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.ResetAliases();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Remove_StopsTheSourceFromBubblingFurtherChanges()
    {
        var source = _registry.GetOrCreate<ConnectionSource>("connection");
        _registry.Remove(source);
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        source.Set(connected: true, DateTimeOffset.UnixEpoch);

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Clear_StopsSourcesFromBubblingFurtherChanges()
    {
        var source = _registry.GetOrCreate<ConnectionSource>("connection");
        _registry.Clear();
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        source.Set(connected: true, DateTimeOffset.UnixEpoch);

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Load_DetachesPreviousSourcesAndAttachesLoadedOnes()
    {
        var previous = _registry.GetOrCreate<ConnectionSource>("connection");
        var loaded = new ConnectionSource { Id = "connection2" };
        _registry.Load(new IIntervalSource[] { loaded });
        var fromPrevious = 0;
        var fromLoaded = 0;
        _registry.Changed += (_, _) => fromPrevious++;
        previous.Set(connected: true, DateTimeOffset.UnixEpoch);
        Assert.That(fromPrevious, Is.EqualTo(0), "a source replaced by Load must no longer bubble");

        _registry.Changed += (_, _) => fromLoaded++;
        loaded.Set(connected: true, DateTimeOffset.UnixEpoch);
        Assert.That(fromLoaded, Is.GreaterThanOrEqualTo(1), "a loaded source must bubble its changes");
    }

    [Test]
    public void Load_RemembersTheLoadedOrderForLaterRuns()
    {
        _registry.Load(new IIntervalSource[]
        {
            new ConnectionSource { Id = "connection" },
            new FeedbackSensorSource { Id = "sensor:1" },
        });

        var nextRun = new IntervalSourceRegistry(_store);
        var sensor = nextRun.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var connection = nextRun.GetOrCreate<ConnectionSource>("connection");

        Assert.That(nextRun.Sources, Is.EqualTo(new IIntervalSource[] { connection, sensor }));
    }

    [Test]
    public void Load_BindsTheRegistryStoreToLoadedSources()
    {
        var loaded = new FeedbackSensorSource { Id = "sensor:1", Sensor = new SensorKey(1, 1) };
        _registry.Load(new IIntervalSource[] { loaded });

        loaded.Label = "Yard 3";

        Assert.That(_store.GetValue<string>("sensor:1/label"), Is.EqualTo("Yard 3"));
    }

    [Test]
    public void Remove_DropsTheSource()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        _registry.Remove(source);

        Assert.That(_registry.Sources, Is.Empty);
    }

    [Test]
    public void Clear_RemovesAllSources()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        _registry.Clear();

        Assert.That(_registry.Sources, Is.Empty);
    }

    [Test]
    public void Load_ReplacesContentsWithGivenSources()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var restored = new ConnectionSource { Id = "connection" };

        _registry.Load(new IIntervalSource[] { restored });

        Assert.That(_registry.Sources, Is.EqualTo(new IIntervalSource[] { restored }));
    }

    [Test]
    public void GetOrCreate_AfterLoad_AppendsBelowLoaded()
    {
        _registry.Load(new IIntervalSource[]
        {
            new ConnectionSource { Id = "connection" },
            new FeedbackSensorSource { Id = "sensor:9" },
        });

        var added = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(_registry.Sources, Is.EqualTo(new IIntervalSource[]
        {
            _registry.Find("connection")!,
            _registry.Find("sensor:9")!,
            added,
        }));
    }

    [Test]
    public void GetOrCreate_NewKey_RunsInitializerOnTheCreatedSource()
    {
        var created = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", source => source.Sensor = new SensorKey(4, 2));

        Assert.That(created.Sensor, Is.EqualTo(new SensorKey(4, 2)));
    }

    [Test]
    public void GetOrCreate_ExistingKey_DoesNotRunInitializer()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", source => source.Sensor = new SensorKey(4, 2));

        var ran = false;
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", _ => ran = true);

        Assert.That(ran, Is.False);
    }

    [Test]
    public void GetOrCreate_BindsTheRegistryStoreToTheSource()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1", s => s.Sensor = new SensorKey(1, 1));

        source.Label = "Yard 3";

        Assert.That(_store.GetValue<string>("sensor:1/label"), Is.EqualTo("Yard 3"));
    }

    [Test]
    public void Find_ExistingKey_ReturnsTheSource()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(_registry.Find("sensor:1"), Is.SameAs(source));
    }

    [Test]
    public void Find_UnknownKey_ReturnsNull()
    {
        Assert.That(_registry.Find("sensor:1"), Is.Null);
    }

    [Test]
    public void GetOrCreate_NewKey_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void GetOrCreate_ExistingKey_DoesNotRaiseChanged()
    {
        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");

        Assert.That(raised, Is.EqualTo(0));
    }

    [Test]
    public void Remove_RaisesChanged()
    {
        var source = _registry.GetOrCreate<FeedbackSensorSource>("sensor:1");
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.Remove(source);

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Clear_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.Clear();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void Load_RaisesChanged()
    {
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        _registry.Load(new IIntervalSource[] { new ConnectionSource { Id = "connection" } });

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void SourceChanged_BubblesAsRegistryChanged()
    {
        var source = _registry.GetOrCreate<ConnectionSource>("connection");
        var raised = 0;
        _registry.Changed += (_, _) => raised++;

        source.Set(connected: true, DateTimeOffset.UnixEpoch);

        Assert.That(raised, Is.GreaterThanOrEqualTo(1));
    }
}
