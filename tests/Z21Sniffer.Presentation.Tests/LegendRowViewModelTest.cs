using NUnit.Framework;
using Z21Sniffer.Core.Recording;
using Z21Sniffer.Presentation.ViewModels;

namespace Z21Sniffer.Presentation.Tests;

[TestFixture]
public class LegendRowViewModelTest
{
    [Test]
    public void ExposesSourceAndContent()
    {
        var source = new FeedbackSensorSource { Id = "sensor:1.1" };
        var content = new object();

        var row = new LegendRowViewModel(source, content);

        Assert.That(row.Source, Is.SameAs(source));
        Assert.That(row.Content, Is.SameAs(content));
    }
}
