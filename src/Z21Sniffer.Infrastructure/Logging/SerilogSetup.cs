using Serilog;

namespace Z21Sniffer.Infrastructure.Logging;

public sealed class SerilogSetup
{
    public LoggerConfiguration Create(string logsDirectory)
    {
        Directory.CreateDirectory(logsDirectory);
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logsDirectory, "z21sniffer-.log"),
                rollingInterval: RollingInterval.Day)
            .WriteTo.Debug();
    }
}
