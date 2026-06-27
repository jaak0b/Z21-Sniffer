using NUnit.Framework;
using Z21Sniffer.Core.Model;
using Z21Sniffer.Infrastructure.Persistence;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class JsonSettingsStoreTest : TempDirectoryTest
{
    private string SettingsPath => Path.Combine(TempDir, "settings.json");

    [Test]
    public void Load_WhenFileMissing_ReturnsDefaults()
    {
        var store = new JsonSettingsStore(SettingsPath);

        var settings = store.Load();

        Assert.That(settings.Host, Is.EqualTo("192.168.0.111"));
        Assert.That(settings.Port, Is.EqualTo(21105));
        Assert.That(settings.Language, Is.EqualTo("en"));
    }

    [Test]
    public void Save_CreatesMissingDirectory()
    {
        var path = Path.Combine(TempDir, "nested", "deep", "settings.json");

        new JsonSettingsStore(path).Save(new AppSettings("h", 1, "en"));

        Assert.That(File.Exists(path), Is.True);
    }

    [Test]
    public void Save_ThenLoad_RoundTripsSettings()
    {
        var store = new JsonSettingsStore(SettingsPath);
        var settings = new AppSettings("10.0.0.42", 21106, "de", McpPort: 9000, DarkTheme: true)
            with { CaptureTrainData = true };

        store.Save(settings);
        var loaded = new JsonSettingsStore(SettingsPath).Load();

        Assert.That(loaded.Host, Is.EqualTo("10.0.0.42"));
        Assert.That(loaded.Port, Is.EqualTo(21106));
        Assert.That(loaded.Language, Is.EqualTo("de"));
        Assert.That(loaded.McpPort, Is.EqualTo(9000));
        Assert.That(loaded.DarkTheme, Is.True);
        Assert.That(loaded.CaptureTrainData, Is.True);
    }
}
