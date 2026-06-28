using NUnit.Framework;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.Timeline.Series;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class SteppedSeriesShapeTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private readonly SteppedSeriesShape _shape = new();

    private static SeriesPoint Point(int seconds, double value) => new(T0.AddSeconds(seconds), value);

    [Test]
    public void ValueAt_EmptySeries_IsNull() =>
        Assert.That(_shape.ValueAt(Array.Empty<SeriesPoint>(), T0), Is.Null);

    [Test]
    public void ValueAt_BeforeFirstPoint_IsNull() =>
        Assert.That(_shape.ValueAt(new[] { Point(10, 80) }, T0.AddSeconds(5)), Is.Null);

    [Test]
    public void ValueAt_BetweenTwoPoints_HoldsThePreviousValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 40), Point(10, 90) }, T0.AddSeconds(5)), Is.EqualTo(40));

    [Test]
    public void ValueAt_ExactlyOnAPoint_ReturnsThatValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 40), Point(10, 90) }, T0.AddSeconds(10)), Is.EqualTo(90));

    [Test]
    public void ValueAt_AfterLastPoint_HoldsLastValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 40), Point(10, 90) }, T0.AddSeconds(20)), Is.EqualTo(90));

    [Test]
    public void BuildPath_InsertsAHorizontalThenVerticalCornerBetweenEachPoint()
    {
        var corners = new[] { new PlotPoint(0, 5), new PlotPoint(10, 2) };

        Assert.That(_shape.BuildPath(corners), Is.EqualTo(new[]
        {
            new PlotPoint(0, 5),
            new PlotPoint(10, 5),
            new PlotPoint(10, 2),
        }));
    }

    [Test]
    public void BuildPath_EmptyCorners_ReturnsEmpty() =>
        Assert.That(_shape.BuildPath(Array.Empty<PlotPoint>()), Is.Empty);
}
