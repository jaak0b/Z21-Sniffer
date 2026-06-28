using NUnit.Framework;
using Z21Sniffer.Infrastructure;

namespace Z21Sniffer.Infrastructure.Tests;

[TestFixture]
public class AppPathsTest
{
    private readonly AppPaths _paths = new(Path.Combine("base", "data"));

    [Test]
    public void SettingsFile_IsSettingsJsonUnderDataDirectory() =>
        Assert.That(_paths.SettingsFile, Is.EqualTo(Path.Combine("base", "data", "settings.json")));

    [Test]
    public void KeyValueFile_IsKvJsonUnderDataDirectory() =>
        Assert.That(_paths.KeyValueFile, Is.EqualTo(Path.Combine("base", "data", "kv.json")));

    [Test]
    public void LogsDirectory_IsLogsUnderDataDirectory() =>
        Assert.That(_paths.LogsDirectory, Is.EqualTo(Path.Combine("base", "data", "logs")));

    [Test]
    public void SessionsDirectory_IsSessionsUnderDataDirectory() =>
        Assert.That(_paths.SessionsDirectory, Is.EqualTo(Path.Combine("base", "data", "sessions")));
}
