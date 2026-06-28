using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class TrackPowerIntervalChartDrawingStrategyTest
{
    private static readonly BarRect Rect = new(10, 20, 300, 26);
    private static readonly ChartViewport Viewport = new(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(10), 1000);

    private TrackPowerIntervalChartDrawingStrategy _strategy = null!;
    private TrackPowerSource _source = null!;
    private RecordingTimelineSurface _surface = null!;

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _strategy = new TrackPowerIntervalChartDrawingStrategy();
        _source = new TrackPowerSource();
        _surface = new RecordingTimelineSurface();
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private void Draw(TrackPowerStatus status, BarContentContext ctx)
    {
        var interval = new TrackPowerInterval { Status = status, Start = DateTimeOffset.UnixEpoch };
        _strategy.Draw(_source, interval, _surface, Rect, ctx, Viewport);
    }

    private static BarContentContext Content(bool show) =>
        new(ShowContent: show, FullDuration: TimeSpan.FromSeconds(3));

    [Test]
    public void LaneHeight_IsFixed()
    {
        Assert.That(_strategy.LaneHeight(0), Is.EqualTo(26));
        Assert.That(_strategy.LaneHeight(1), Is.EqualTo(26));
    }

    [TestCase(TrackPowerStatus.On, TimelineInkKeys.TrackPowerOn)]
    [TestCase(TrackPowerStatus.Off, TimelineInkKeys.TrackPowerOff)]
    [TestCase(TrackPowerStatus.Short, TimelineInkKeys.TrackPowerShort)]
    [TestCase(TrackPowerStatus.Programming, TimelineInkKeys.TrackPowerProgramming)]
    public void Draw_FillsInkForStatus(TrackPowerStatus status, string inkKey)
    {
        Draw(status, Content(show: false));

        Assert.That(_surface.Fills, Has.Exactly(1).Matches<RecordingTimelineSurface.FillOp>(f => f.Ink.Key == inkKey));
    }

    [Test]
    public void Draw_WithContent_On_ShowsNameAndDurationWithLightText()
    {
        Draw(TrackPowerStatus.On, Content(show: true));

        var text = _surface.Texts.Single();
        Assert.That(text.Text, Is.EqualTo($"{LocalizationService.Instance["TrackPowerOnState"]} · 3 s"));
        Assert.That(text.Ink.Key, Is.EqualTo(TimelineInkKeys.TrackPowerText));
        Assert.That(text.X, Is.EqualTo(Rect.X + 5));
        Assert.That(text.Y, Is.EqualTo(Rect.Y + Rect.H / 2));
    }

    [Test]
    public void Draw_WithContent_Off_UsesDarkOffText()
    {
        Draw(TrackPowerStatus.Off, Content(show: true));

        var text = _surface.Texts.Single();
        Assert.That(text.Text, Is.EqualTo($"{LocalizationService.Instance["TrackPowerOffState"]} · 3 s"));
        Assert.That(text.Ink.Key, Is.EqualTo(TimelineInkKeys.TrackPowerOffText));
    }

    [Test]
    public void Draw_WithContent_RoundsDurationToThreeDecimals()
    {
        Draw(TrackPowerStatus.On, new BarContentContext(ShowContent: true, FullDuration: TimeSpan.FromSeconds(1.23456)));

        Assert.That(_surface.Texts.Single().Text, Is.EqualTo($"{LocalizationService.Instance["TrackPowerOnState"]} · 1.235 s"));
    }

    [Test]
    public void Draw_WithContent_Short_ShowsShortName()
    {
        Draw(TrackPowerStatus.Short, Content(show: true));

        Assert.That(_surface.Texts.Single().Text, Is.EqualTo($"{LocalizationService.Instance["TrackPowerShortState"]} · 3 s"));
    }

    [Test]
    public void Draw_WithContent_Programming_ShowsProgrammingName()
    {
        Draw(TrackPowerStatus.Programming, Content(show: true));

        Assert.That(_surface.Texts.Single().Text, Is.EqualTo($"{LocalizationService.Instance["TrackPowerProgrammingState"]} · 3 s"));
    }

    [Test]
    public void Probe_DescribesTheStatusAndDurationAtTheCursorTime()
    {
        var interval = new TrackPowerInterval { Status = TrackPowerStatus.Short, Start = DateTimeOffset.UnixEpoch };

        var text = _strategy.Probe(_source, interval, DateTimeOffset.UnixEpoch.AddSeconds(3));

        Assert.That(text, Is.EqualTo($"{LocalizationService.Instance["TrackPowerShortState"]} · 3 s"));
    }

    [Test]
    public void Draw_WithoutContent_DrawsNoText()
    {
        Draw(TrackPowerStatus.On, Content(show: false));

        Assert.That(_surface.Texts, Is.Empty);
    }
}
