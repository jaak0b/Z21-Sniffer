using Autofac.Features.Indexed;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.ViewModels;

public enum SourceVisibilityState
{
    None,
    Some,
    All,
}

public sealed class SourceVisibilityItem
{
    public SourceVisibilityItem(string label, IIntervalSource source)
    {
        Label = label;
        Source = source;
    }

    public string Label { get; }

    public IIntervalSource Source { get; }

    public bool IsVisible
    {
        get => Source.IsVisible;
        set => Source.IsVisible = value;
    }

    public void Toggle() => Source.IsVisible = !Source.IsVisible;
}

public sealed class SourceVisibilityGroup
{
    public SourceVisibilityGroup(string typeLabel, string iconGeometry, IReadOnlyList<SourceVisibilityItem> sources)
    {
        TypeLabel = typeLabel;
        IconGeometry = iconGeometry;
        Sources = sources;
    }

    public string TypeLabel { get; }

    public string IconGeometry { get; }

    public IReadOnlyList<SourceVisibilityItem> Sources { get; }

    public SourceVisibilityState State =>
        Sources.All(item => item.IsVisible) ? SourceVisibilityState.All
        : Sources.Any(item => item.IsVisible) ? SourceVisibilityState.Some
        : SourceVisibilityState.None;

    public void Toggle()
    {
        var makeVisible = State != SourceVisibilityState.All;
        foreach (var item in Sources) item.IsVisible = makeVisible;
    }
}

public sealed class SourceVisibilityViewModel
{
    private readonly IIntervalSourceRegistry _registry;
    private readonly IIndex<Type, IIntervalLegendDrawingStrategy> _legend;

    public SourceVisibilityViewModel(IIntervalSourceRegistry registry, IIndex<Type, IIntervalLegendDrawingStrategy> legend)
    {
        _registry = registry;
        _legend = legend;
    }

    public int ShownCount => _registry.Sources.Count(source => source.IsVisible);

    public int TotalCount => _registry.Sources.Count;

    public IReadOnlyList<SourceVisibilityGroup> BuildTree(string filter = "")
    {
        var trimmed = filter.Trim();
        var groups = new List<(Type Type, string Label, string Icon, List<SourceVisibilityItem> Items)>();

        foreach (var source in _registry.Sources)
        {
            var strategy = _legend[source.IntervalType];
            var typeLabel = strategy.TypeLabel;
            var rowLabel = strategy.RowLabel(source);
            if (!Matches(trimmed, typeLabel, rowLabel)) continue;

            var bucket = groups.FirstOrDefault(group => group.Type == source.IntervalType);
            if (bucket.Items is null)
            {
                bucket = (source.IntervalType, typeLabel, strategy.IconGeometry, new List<SourceVisibilityItem>());
                groups.Add(bucket);
            }

            bucket.Items.Add(new SourceVisibilityItem(rowLabel, source));
        }

        return groups.Select(group => new SourceVisibilityGroup(group.Label, group.Icon, group.Items)).ToList();
    }

    public void ShowAll()
    {
        foreach (var source in _registry.Sources) source.IsVisible = true;
    }

    public void HideAll()
    {
        foreach (var source in _registry.Sources) source.IsVisible = false;
    }

    private static bool Matches(string filter, string typeLabel, string rowLabel) =>
        filter.Length == 0
        || typeLabel.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        || rowLabel.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
}
