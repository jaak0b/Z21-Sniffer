using NUnit.Framework;
using Z21Sniffer.Infrastructure.Persistence;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class FileLogTextStoreTest
{
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "z21sniffer-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    [Test]
    public void Save_WritesTextToPath()
    {
        var path = Path.Combine(_dir, "log.txt");
        var text = "line one\r\nline two";

        new FileLogTextStore().Save(text, path);

        Assert.That(File.ReadAllText(path), Is.EqualTo(text));
    }
}
