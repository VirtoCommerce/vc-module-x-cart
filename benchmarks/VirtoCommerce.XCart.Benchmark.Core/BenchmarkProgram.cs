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
/// over <see cref="Run"/>, passing its OWN assembly: the concrete benchmark subclasses (one per Core
/// <c>*BenchmarksBase</c>, source-generated, each baking the runner's <see cref="ICartBenchmarkSetup"/>)
/// live in the runner exe. So <see cref="BenchmarkSwitcher.FromAssembly"/> discovers them there, and the
/// stock out-of-process toolchain rebuilds the runner's own <c>.csproj</c> for the child process — no
/// custom toolchain, no in-process mode, no process-global state.
/// </summary>
public static class BenchmarkProgram
{
    /// <summary>
    /// Parses the one opt-in option (<c>--baseline-src</c>), builds the config, and runs the concrete
    /// benchmarks discovered in <paramref name="benchmarkAssembly"/>. Job selection is BenchmarkDotNet's
    /// own <c>--job Dry|Short|Default</c> CLI flag (forwarded untouched to the switcher) — there are no
    /// custom <c>--smoke</c>/<c>--short</c> aliases, so every module's runner shares one job-selection
    /// mechanism and the perf-benchmark helpers don't have to special-case a runner dialect.
    /// </summary>
    public static void Run(Assembly benchmarkAssembly, string[] args)
    {
        // Opt-in before/after comparison: `--baseline-src <src>` adds a "before" job that rebuilds the
        // benchmarked source from <src> (a git worktree on the baseline revision) alongside the current
        // "after" source, yielding Ratio / Alloc-Ratio in one run. The `/p:BaselineSrc` MSBuild property
        // (named by role, not module, so every module's benchmark Core shares it) flows from the
        // generated child build through the runner's ProjectReference graph into the source references.
        // When absent the config carries no job, so BenchmarkDotNet uses its `--job` CLI flag (default
        // Job.Default).
        var (baselineSrc, rest) = ExtractOption(args, "--baseline-src");

        var config = ManualConfig.Create(DefaultConfig.Instance).AddColumn(CategoriesColumn.Default);
        if (baselineSrc is not null)
        {
            // --baseline-src needs two jobs with identical settings (only the source differs), so it
            // pins Job.Default for both rather than reading the --job flag — it is the "quick eyeball
            // Ratio" path, where a measured job is what you want anyway.
            config = config
                .AddJob(Job.Default.WithMsBuildArguments($"/p:BaselineSrc={baselineSrc}").WithId("before").AsBaseline())
                .AddJob(Job.Default.WithId("after"));
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
