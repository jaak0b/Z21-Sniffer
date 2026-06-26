using NUnit.Framework;

namespace Z21Sniffer.Infrastructure.Tests;

public abstract class TempDirectoryTest
{
    protected string TempDir { get; private set; } = null!;

    [SetUp]
    public void CreateTempDir()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "z21sniffer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDir);
    }

    [TearDown]
    public void DeleteTempDir()
    {
        if (Directory.Exists(TempDir)) Directory.Delete(TempDir, recursive: true);
    }
}
