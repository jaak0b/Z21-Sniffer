using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.Localization;
using Z21Sniffer.Presentation.Timeline;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class ConnectionIntervalChartDrawingStrategyTest
{
    private static readonly BarRect Rect = new(10, 20, 300, 26);

    private ConnectionIntervalChartDrawingStrategy _strategy = null!;
    private ConnectionSource _source = null!;
    private RecordingTimelineSurface _surface = null!;

    [SetUp]
    public void SetUp()
    {
        LocalizationService.Instance.Apply("en");
        _strategy = new ConnectionIntervalChartDrawingStrategy();
        _source = new ConnectionSource();
        _surface = new RecordingTimelineSurface();
    }

    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    private static readonly ChartViewport Viewport = new(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(10), 1000);

    private void Draw(bool connected, BarContentContext ctx)
    {
        var interval = new ConnectionInterval { Connected = connected, Start = DateTimeOffset.UnixEpoch };
        _strategy.Draw(_source, interval, _surface, Rect, ctx, Viewport);
    }

    [Test]
    public void Draw_Connected_FillsConnectedInk()
    {
        Draw(connected: true, new BarContentContext(ShowContent: false, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Fills, Has.Exactly(1).Matches<RecordingTimelineSurface.FillOp>(
            f => f.Ink.Key == TimelineInkKeys.Connected));
    }

    [Test]
    public void Draw_Disconnected_FillsDisconnectedInk()
    {
        Draw(connected: false, new BarContentContext(ShowContent: false, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Fills, Has.Exactly(1).Matches<RecordingTimelineSurface.FillOp>(
            f => f.Ink.Key == TimelineInkKeys.Disconnected));
    }

    [Test]
    public void Draw_WithContent_Connected_ShowsConnectedStateAndDuration()
    {
        Draw(connected: true, new BarContentContext(ShowContent: true, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        var text = _surface.Texts.Single();
        Assert.That(text.Text, Is.EqualTo($"{LocalizationService.Instance["Connected"]} · 3 s"));
        Assert.That(text.Ink.Key, Is.EqualTo(TimelineInkKeys.ConnectionText));
        Assert.That(text.X, Is.EqualTo(Rect.X + 5));
        Assert.That(text.Y, Is.EqualTo(Rect.Y + Rect.H / 2));
    }

    [Test]
    public void Draw_WithContent_RoundsDurationToThreeDecimals()
    {
        Draw(connected: true, new BarContentContext(ShowContent: true, Highlighted: false, FullDuration: TimeSpan.FromSeconds(1.23456)));

        Assert.That(_surface.Texts.Single().Text, Is.EqualTo($"{LocalizationService.Instance["Connected"]} · 1.235 s"));
    }

    [Test]
    public void Draw_WithContent_Disconnected_ShowsDisconnectedStateAndDuration()
    {
        Draw(connected: false, new BarContentContext(ShowContent: true, Highlighted: false, FullDuration: TimeSpan.FromSeconds(12.5)));

        Assert.That(_surface.Texts, Has.Exactly(1).Matches<RecordingTimelineSurface.TextOp>(
            t => t.Text == $"{LocalizationService.Instance["Disconnected"]} · 12.5 s" && t.Ink.Key == TimelineInkKeys.ConnectionText));
    }

    [Test]
    public void Draw_WithoutContent_DrawsNoText()
    {
        Draw(connected: true, new BarContentContext(ShowContent: false, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Texts, Is.Empty);
    }

    [Test]
    public void Probe_DescribesTheConnectionStateAndDurationAtTheCursorTime()
    {
        var interval = new ConnectionInterval { Connected = false, Start = DateTimeOffset.UnixEpoch };

        var text = _strategy.Probe(_source, interval, DateTimeOffset.UnixEpoch.AddSeconds(3));

        Assert.That(text, Is.EqualTo($"{LocalizationService.Instance["Disconnected"]} · 3 s"));
    }
}
