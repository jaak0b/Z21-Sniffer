using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter("Configuration to build — default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Parameter("Minimum acceptable line coverage percentage for the Coverage gate")]
    readonly double CoverageThreshold = 95;

    [Parameter("Git baseline that scopes mutation testing to your local changes. Defaults to 'HEAD' — only the code you have changed since your last commit. Mutating across the whole codebase is intentionally not supported — it is far too slow.")]
    readonly string Since = "HEAD";

    AbsolutePath SourceRoot => RootDirectory / "src";
    AbsolutePath TestProjectsRoot => RootDirectory / "tests";
    AbsolutePath CoverageDirectory => RootDirectory / "TestResults" / "coverage";
    AbsolutePath CoverageReportDirectory => RootDirectory / "TestResults" / "CoverageReport";
    AbsolutePath CoverageSettings => RootDirectory / "coverlet.runsettings";
    AbsolutePath TestSettings => RootDirectory / "tests.runsettings";
    AbsolutePath Solution => RootDirectory / "Z21Sniffer.slnx";
    AbsolutePath DesktopProject => SourceRoot / "Z21Sniffer.UI.Desktop" / "Z21Sniffer.UI.Desktop.csproj";

    string[] TestProjects =>
    [
        "Z21Sniffer.Core.Tests",
        "Z21Sniffer.Infrastructure.Tests",
        "Z21Sniffer.Presentation.Tests",
        "Z21Sniffer.Mcp.Tests",
        "Z21Sniffer.UI.Tests"
    ];

    string[] MutationProjects =>
    [
        "Z21Sniffer.Core",
        "Z21Sniffer.Infrastructure",
        "Z21Sniffer.Presentation",
        "Z21Sniffer.Mcp"
    ];

    Target Restore => _ => _
        .Executes(() => DotNetRestore(s => s.SetProjectFile(Solution)));

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(s => s
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration)
            .EnableNoRestore()));

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() => DotNetTest(s => s
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetSettingsFile(TestSettings)
                .CombineWith(TestProjects, (settings, project) => settings
                    .SetProjectFile(TestProjectsRoot / project / $"{project}.csproj")),
            degreeOfParallelism: TestProjects.Length,
            completeOnFailure: true));

    Target Coverage => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            CoverageDirectory.CreateOrCleanDirectory();
            CoverageReportDirectory.CreateOrCleanDirectory();

            DotNetTest(s => s
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetDataCollector("XPlat Code Coverage")
                    .SetSettingsFile(CoverageSettings)
                    .SetResultsDirectory(CoverageDirectory)
                    .CombineWith(TestProjects, (settings, project) => settings
                        .SetProjectFile(TestProjectsRoot / project / $"{project}.csproj")),
                degreeOfParallelism: TestProjects.Length,
                completeOnFailure: true);

            var coverageFiles = CoverageDirectory.GlobFiles("**/coverage.cobertura.xml");
            Assert.True(coverageFiles.Count > 0, "No coverage files were produced.");

            DotNet(
                $"reportgenerator " +
                $"\"-reports:{CoverageDirectory / "**" / "coverage.cobertura.xml"}\" " +
                $"\"-targetdir:{CoverageReportDirectory}\" " +
                $"\"-reporttypes:TextSummary;Cobertura;Html\"",
                workingDirectory: RootDirectory);

            var merged = CoverageReportDirectory / "Cobertura.xml";
            var percent = Math.Round(ReadLineRate(merged) * 100, 2);
            Log.Information("Merged line coverage: {Percent}% (threshold {Threshold}%)", percent, CoverageThreshold);
            Assert.True(percent >= CoverageThreshold,
                $"Line coverage {percent}% is below the required {CoverageThreshold}%.");
        });

    static double ReadLineRate(AbsolutePath coberturaFile)
    {
        var coverage = XDocument.Load(coberturaFile).Root!;
        var covered = coverage.Attribute("lines-covered");
        var valid = coverage.Attribute("lines-valid");
        if (covered is not null && valid is not null &&
            double.TryParse(valid.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var validCount) &&
            validCount > 0 &&
            double.TryParse(covered.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var coveredCount))
        {
            return coveredCount / validCount;
        }

        var lineRate = coverage.Attribute("line-rate");
        return lineRate is not null &&
               double.TryParse(lineRate.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate)
            ? rate
            : 0;
    }

    Target Mutate => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var changed = ChangedMutableSourceFiles();
            if (changed.Count == 0)
            {
                Log.Information("Mutate: no changed source files since {Since}; nothing to mutate.", Since);
                return;
            }

            foreach (var project in MutationProjects)
            {
                var prefix = $"src/{project}/";
                var relative = changed
                    .Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Substring(prefix.Length))
                    .ToArray();
                if (relative.Length == 0) continue;

                var csproj = SourceRoot / project / $"{project}.csproj";
                var testProject = TestProjectsRoot / $"{project}.Tests" / $"{project}.Tests.csproj";
                var patterns = string.Join(" ", relative.Select(f => $"--mutate **/{System.IO.Path.GetFileName(f)}"));
                var arguments = $"stryker -p \"{csproj}\" --test-project \"{testProject}\" {patterns}";

                var dotnet = ToolPathResolver.GetPathExecutable("dotnet");
                var process = ProcessTasks.StartProcess(dotnet, arguments, RootDirectory);
                process.WaitForExit();

                var text = string.Join("\n", process.Output.Select(o => o.Text));
                if (text.Contains("unable to calculate a mutation score", StringComparison.OrdinalIgnoreCase) ||
                    !text.Contains("final mutation score", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail(
                        $"Mutate: Stryker tested no mutants for {project} ({relative.Length} changed file(s): "
                        + $"{string.Join(", ", relative.Select(System.IO.Path.GetFileName))}). "
                        + "The changed files were Excluded instead of mutated — the mutation gate did not actually run. "
                        + "Check the --mutate glob arguments reaching Stryker.");
                }

                process.AssertZeroExitCode();
            }
        });

    Target RunDesktop => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Assert.True(EnvironmentInfo.IsWin, "RunDesktop targets the Windows desktop head.");
            DotNetRun(s => s
                .SetProjectFile(DesktopProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    const string GitEmptyTreeObject = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

    IReadOnlyList<string> ChangedMutableSourceFiles()
    {
        IEnumerable<string> Lines(string arguments) =>
            Git(arguments, workingDirectory: RootDirectory, logOutput: false)
                .Where(o => o.Type == OutputType.Std)
                .Select(o => o.Text);

        return Lines($"diff --name-only {ResolveDiffBase()}")
            .Concat(Lines("ls-files --others --exclude-standard"))
            .Select(p => p.Trim().Replace('\\', '/'))
            .Where(p => p.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                        && p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        && !IsExcludedFromMutation(p))
            .Distinct()
            .ToList();
    }

    string ResolveDiffBase()
    {
        try
        {
            Git($"rev-parse --verify {Since}", workingDirectory: RootDirectory, logOutput: false);
            return Since;
        }
        catch (Exception exception)
        {
            Log.Information("Mutate: {Since} does not resolve ({Message}); diffing against the empty tree (initial commit).",
                Since, exception.Message);
            return GitEmptyTreeObject;
        }
    }

    static bool IsExcludedFromMutation(string path) =>
        path.EndsWith(".axaml.cs", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/Views/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/Controls/", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("Module.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("KestrelMcpServerController.cs", StringComparison.OrdinalIgnoreCase);
}
