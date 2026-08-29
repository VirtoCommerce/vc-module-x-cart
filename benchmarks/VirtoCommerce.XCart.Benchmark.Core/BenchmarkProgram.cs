using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Shared entry-point plumbing for the XCart benchmark runners. Each runner's <c>Main</c> is a one-liner
/// over <see cref="Run"/>, passing its OWN assembly — that is where the source-generated concrete
/// subclasses live, so <see cref="BenchmarkSwitcher.FromAssembly"/> finds them. README §"Layout and
/// toolchain" has the model.
/// </summary>
public static class BenchmarkProgram
{
    /// <summary>
    /// Parses the one opt-in option (<c>--baseline-src</c>), builds the config, and runs the concrete
    /// benchmarks discovered in <paramref name="benchmarkAssembly"/>. Every other argument —
    /// <c>--job</c> included — is forwarded to BenchmarkDotNet untouched; there is no runner dialect.
    /// README §"Comparing before/after a change" documents the comparison workflow.
    /// </summary>
    public static void Run(Assembly benchmarkAssembly, string[] args)
    {
        var (baselineSrc, rest) = ExtractOption(args, "--baseline-src");

        var config = ManualConfig.Create(DefaultConfig.Instance).AddColumn(CategoriesColumn.Default);
        if (baselineSrc is not null)
        {
            // before+after differ ONLY by source, so --job is consumed here rather than left for the
            // switcher — forwarding it would append a third, unpaired job.
            var (jobName, restAfterJob) = ExtractOption(rest, "--job");
            rest = restAfterJob;
            var normalized = (jobName ?? "dry").ToLowerInvariant();
            var baselineJob = normalized switch
            {
                "dry" => Job.Dry,
                "short" => Job.ShortRun,
                "default" or "measured" => Job.Default,
                _ => throw new ArgumentException($"--job must be Dry|Short|Default with --baseline-src; got '{jobName}'."),
            };
            if (normalized is not ("default" or "measured"))
            {
                Console.Error.WriteLine($"// --baseline-src on --job {normalized}: Alloc Ratio is exact; the time " +
                    "Ratio is directional only (not a verdict) — re-run with `--job Default` for a trustworthy Mean.");
            }
            config = config
                .AddJob(baselineJob.WithMsBuildArguments($"/p:BaselineSrc=\"{baselineSrc}\"").WithId("before").AsBaseline())
                .AddJob(baselineJob.WithId("after"));
        }

        BenchmarkSwitcher.FromAssembly(benchmarkAssembly).Run(rest, config);
    }

    // Removes "<name> <value>" from args and returns the value (null if the flag is absent), so the
    // remaining args pass through to BenchmarkSwitcher untouched.
    private static (string, string[]) ExtractOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0)
        {
            return (null, args);
        }

        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{name} requires a path argument.");
        }

        var value = args[index + 1];
        var rest = args.Where((_, i) => i != index && i != index + 1).ToArray();

        return (value, rest);
    }
}
