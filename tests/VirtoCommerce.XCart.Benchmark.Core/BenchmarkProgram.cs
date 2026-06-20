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
/// <c>*BenchmarksBase</c>, source-generated, each baking the runner's <see cref="ICartModuleBenchmarkSetup"/>)
/// live in the runner exe. So <see cref="BenchmarkSwitcher.FromAssembly"/> discovers them there, and the
/// stock out-of-process toolchain rebuilds the runner's own <c>.csproj</c> for the child process — no
/// custom toolchain, no in-process mode, no process-global state.
/// </summary>
public static class BenchmarkProgram
{
    /// <summary>
    /// Parses the opt-in flags (<c>--baseline-src</c>, <c>--smoke</c>, <c>--short</c>), builds the config,
    /// and runs the concrete benchmarks discovered in <paramref name="benchmarkAssembly"/>.
    /// </summary>
    public static void Run(Assembly benchmarkAssembly, string[] args)
    {
        // Opt-in before/after comparison: `--baseline-src <src>` adds a "before" job that rebuilds the
        // benchmarked source from <src> (a git worktree on the baseline revision) alongside the current
        // "after" source, yielding Ratio / Alloc-Ratio in one run. The `/p:XCartSrc` MSBuild property
        // flows from the generated child build through the runner's ProjectReference graph into the
        // XCart source references. When absent the config is a single default job.
        var (baselineSrc, afterBaseline) = ExtractOption(args, "--baseline-src");

        // --smoke = Job.Dry (run each case once — correctness check). --short = Job.ShortRun (bounded,
        // fast-but-real). Both run on the stock toolchain: the concrete subclasses live in the runner
        // exe whose .csproj name matches its assembly name, so BDN's default resolver locates the project.
        var (smoke, afterSmoke) = ExtractFlag(afterBaseline, "--smoke");
        var (shortJob, rest) = ExtractFlag(afterSmoke, "--short");

        Job baseJob;
        if (smoke)
        {
            baseJob = Job.Dry;
        }
        else if (shortJob)
        {
            baseJob = Job.ShortRun;
        }
        else
        {
            baseJob = Job.Default;
        }

        var config = ManualConfig.Create(DefaultConfig.Instance).AddColumn(CategoriesColumn.Default);
        if (baselineSrc is null)
        {
            config = config.AddJob(baseJob);
        }
        else
        {
            config = config
                .AddJob(baseJob.WithMsBuildArguments($"/p:XCartSrc={baselineSrc}").WithId("before").AsBaseline())
                .AddJob(baseJob.WithId("after"));
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

    // Removes a valueless flag from args, returning whether it was present.
    private static (bool, string[]) ExtractFlag(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0)
        {
            return (false, args);
        }

        var rest = args.Where((_, i) => i != index).ToArray();

        return (true, rest);
    }
}
