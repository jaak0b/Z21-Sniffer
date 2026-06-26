using Autofac.Features.Indexed;
using FakeItEasy;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Ports;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.UI.Tests;

internal sealed class FakeIndex<TKey, TValue> : IIndex<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _map;

    public FakeIndex(Dictionary<TKey, TValue> map) => _map = map;

    public TValue this[TKey key] => _map[key];

    public bool TryGetValue(TKey key, out TValue value) => _map.TryGetValue(key, out value!);
}

internal sealed class AlwaysConfirm : IRemovalConfirmation
{
    public Task<bool> ConfirmAsync() => Task.FromResult(true);
}

internal sealed record WorkspaceContext(WorkspaceViewModel Vm, FeedbackSensorIngest Ingest);

internal sealed record TimelineContext(TimelineViewModel Vm, FeedbackSensorIngest Ingest, IntervalSourceRegistry Registry);

internal static class WorkspaceFactory
{
    public static TimelineViewModel BuildTimeline(IClock clock) => BuildTimelineContext(clock).Vm;

    public static TimelineContext BuildTimelineContext(IClock clock)
    {
        var registry = new IntervalSourceRegistry();
        var vm = new TimelineViewModel(registry, ChartStrategies(), LegendStrategies(registry), clock);
        return new TimelineContext(vm, new FeedbackSensorIngest(registry), registry);
    }

    public static WorkspaceViewModel Build(ISettingsStore settings, IClock clock) => BuildContext(settings, clock).Vm;

    public static WorkspaceContext BuildContext(ISettingsStore settings, IClock clock)
    {
        var registry = new IntervalSourceRegistry();
        var ingest = new FeedbackSensorIngest(registry);
        var vm = new WorkspaceViewModel(
            A.Fake<ICommandStationConnectionFactory>(),
            settings,
            A.Fake<ISessionStore>(),
            clock,
            registry,
            ingest,
            ChartStrategies(),
            LegendStrategies(registry),
            A.Fake<IMcpServerController>(),
            A.Fake<IThemeController>(),
            A.Fake<ILogTextStore>(),
            post: action => action(),
            chooseSaveJsonPath: () => Task.FromResult<string?>(null),
            chooseOpenJsonPath: () => Task.FromResult<string?>(null),
            chooseExportLogPath: () => Task.FromResult<string?>(null),
            openSettings: () => Task.CompletedTask);
        return new WorkspaceContext(vm, ingest);
    }

    private static FakeIndex<Type, IIntervalChartDrawingStrategy> ChartStrategies() => new(new()
    {
        [typeof(FeedbackSensorInterval)] = new SensorIntervalChartDrawingStrategy(),
        [typeof(ConnectionInterval)] = new ConnectionIntervalChartDrawingStrategy(),
    });

    private static FakeIndex<Type, IIntervalLegendDrawingStrategy> LegendStrategies(IIntervalSourceRegistry registry) => new(new()
    {
        [typeof(FeedbackSensorInterval)] = new SensorIntervalLegendDrawingStrategy(registry, new AlwaysConfirm()),
        [typeof(ConnectionInterval)] = new ConnectionIntervalLegendDrawingStrategy(),
    });
}
