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

    private void Draw(bool connected, BarContentContext ctx)
    {
        var interval = new ConnectionInterval { Connected = connected, Start = DateTimeOffset.UnixEpoch };
        _strategy.Draw(_source, interval, _surface, Rect, ctx);
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
    public void Draw_WithContent_DrawsLocalizedConnectionLabel()
    {
        Draw(connected: true, new BarContentContext(ShowContent: true, Highlighted: false, FullDuration: TimeSpan.FromSeconds(3)));

        Assert.That(_surface.Texts, Has.Exactly(1).Matches<RecordingTimelineSurface.TextOp>(
            t => t.Text == LocalizationService.Instance["Connection"] && t.Ink.Key == TimelineInkKeys.ConnectionText));
    }
}
