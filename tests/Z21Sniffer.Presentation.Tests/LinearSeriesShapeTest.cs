using NUnit.Framework;
using Z21Sniffer.Presentation.Timeline;
using Z21Sniffer.Presentation.Timeline.Series;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LinearSeriesShapeTest
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private readonly LinearSeriesShape _shape = new();

    private static SeriesPoint Point(int seconds, double value) => new(T0.AddSeconds(seconds), value);

    [Test]
    public void ValueAt_EmptySeries_IsNull() =>
        Assert.That(_shape.ValueAt(Array.Empty<SeriesPoint>(), T0), Is.Null);

    [Test]
    public void ValueAt_BeforeFirstPoint_IsNull() =>
        Assert.That(_shape.ValueAt(new[] { Point(10, 800) }, T0.AddSeconds(5)), Is.Null);

    [Test]
    public void ValueAt_ExactlyOnAPoint_ReturnsThatValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 800), Point(10, 1000) }, T0.AddSeconds(10)), Is.EqualTo(1000));

    [Test]
    public void ValueAt_BetweenTwoPoints_InterpolatesLinearly() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 800), Point(10, 1000) }, T0.AddSeconds(5)), Is.EqualTo(900));

    [Test]
    public void ValueAt_AfterLastPoint_HoldsLastValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 800), Point(10, 1000) }, T0.AddSeconds(20)), Is.EqualTo(1000));

    [Test]
    public void ValueAt_ExactlyAtTheFirstPoint_ReturnsTheFirstValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(2, 800), Point(10, 1000) }, T0.AddSeconds(2)), Is.EqualTo(800));

    [Test]
    public void ValueAt_ExactlyOnAMiddlePoint_ReturnsThatPointsValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 800), Point(5, 950), Point(10, 1000) }, T0.AddSeconds(5)), Is.EqualTo(950));

    [Test]
    public void ValueAt_BetweenTwoPointsSharingATimestamp_ReturnsTheLaterValue() =>
        Assert.That(_shape.ValueAt(new[] { Point(5, 800), Point(5, 1000) }, T0.AddSeconds(5)), Is.EqualTo(1000));

    [Test]
    public void ValueAt_AtAPointFollowedByADuplicateTimestamp_ResolvesToTheReachedSegmentEnd() =>
        Assert.That(_shape.ValueAt(new[] { Point(0, 800), Point(5, 950), Point(5, 1000) }, T0.AddSeconds(5)), Is.EqualTo(950));

    [Test]
    public void BuildPath_ReturnsTheCornersUnchanged()
    {
        var corners = new[] { new PlotPoint(0, 5), new PlotPoint(10, 2), new PlotPoint(20, 8) };

        Assert.That(_shape.BuildPath(corners), Is.EqualTo(corners));
    }
}
