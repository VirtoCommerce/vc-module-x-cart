using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Shared entry-point plumbing for the XCart benchmark runners. A runner's <c>Main</c> is a one-liner
/// over <see cref="Run"/>; the benchmark classes live in this (Core) assembly, so
/// <see cref="BenchmarkSwitcher.FromAssembly"/> targets it regardless of which runner invoked us.
/// </summary>
public static class BenchmarkProgram
{
    /// <summary>
    /// Parses the opt-in <c>--baseline-src</c> flag, builds the config (categories column, plus a
    /// before/after job pair when comparing source revisions), and runs the Core benchmarks.
    /// </summary>
    public static void Run(string[] args)
    {
        // The active module setup registers its AbstractTypeFactory overrides here, in the process
        // that will execute the benchmarks. This is meaningful only for the in-process toolchain
        // (where the benchmarks run in THIS process): a consuming module's runner sets
        // BenchmarkEnvironment.Current before calling Run, so its Leo* overrides are active when the
        // benchmark methods run. Upstream's setup registers nothing (no-op), and the out-of-process
        // toolchain runs benchmarks in a child process that never reaches this call — which is fine,
        // because only the upstream (no-op) setup uses the out-of-process toolchain.
        BenchmarkEnvironment.Current.RegisterTypes();

        // BenchmarkSwitcher forwards CLI args (--filter, --job, --anyCategories, --allCategories, ...)
        // to BenchmarkDotNet. Always pass --filter when running non-interactively to avoid the
        // interactive prompt. The categories column surfaces each benchmark's functional category.
        //
        // Opt-in before/after comparison: `--baseline-src <path-to-src>` adds a second job ("before")
        // that rebuilds XCart.Core/Data from <path> (a git worktree on the baseline revision) and runs
        // it against the current source ("after"), yielding a Ratio column in a single process. The
        // flag is parsed out here and never reaches BDN. When absent the config is unchanged — single
        // default job, every existing flag behaves exactly as before. The /p:XCartSrc MSBuild property
        // flows from the generated child build into this Core project's $(XCartSrc) ProjectReferences.
        var (baselineSrc, afterBaseline) = ExtractOption(args, "--baseline-src");

        // --smoke runs every case once (Job.Dry) for a fast correctness check. Use this instead of the
        // BDN `--job Dry` CLI preset: a CLI `--job` ADDS a job that uses BDN's DEFAULT toolchain, which
        // cannot locate this library's .csproj (see BenchmarkCoreToolchain) and fails to generate.
        var (smoke, afterSmoke) = ExtractFlag(afterBaseline, "--smoke");

        // --short runs a bounded job (Job.Short: a few fixed warmup + measurement iterations) for a
        // fast-but-real measurement. Preferred over the default job for in-process comparison runs:
        // the default job's adaptive iteration count does not converge in-process on the heavier
        // cases (it keeps measuring), whereas allocations stabilize within Short's fixed iterations.
        var (shortJob, afterShort) = ExtractFlag(afterSmoke, "--short");

        // --in-process runs the benchmarks in THIS process (InProcessEmitToolchain) instead of the
        // out-of-process CsProj toolchain. A consuming module (e.g. LEO) that references this library
        // as a NuGet package has no .csproj on disk for the out-of-process toolchain to build, and its
        // BenchmarkEnvironment.Current setup is only visible in-process — so it MUST run in-process.
        // It is also how an upstream baseline is produced on the SAME toolchain a consumer is locked
        // into, which is the only way the time numbers are comparable across runs (allocations are
        // toolchain-robust either way).
        var (inProcess, rest) = ExtractFlag(afterShort, "--in-process");

        if (inProcess && baselineSrc is not null)
        {
            // --baseline-src rebuilds the source from a worktree, which only the out-of-process build
            // does; in-process runs the already-loaded assemblies. The two are mutually exclusive.
            throw new ArgumentException("--in-process cannot be combined with --baseline-src (the before/after rebuild requires the out-of-process toolchain).");
        }

        // In-process: same process, no per-benchmark build. Out-of-process: the Core toolchain resolves
        // this library's .csproj deterministically (BDN's default current-directory search can't find
        // it from a sibling runner exe).
        var toolchain = inProcess ? InProcessEmitToolchain.Instance : BenchmarkCoreToolchain.Instance;

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
            config = config.AddJob(baseJob.WithToolchain(toolchain));
        }
        else
        {
            config = config
                .AddJob(baseJob.WithToolchain(toolchain).WithMsBuildArguments($"/p:XCartSrc={baselineSrc}").WithId("before").AsBaseline())
                .AddJob(baseJob.WithToolchain(toolchain).WithId("after"));
        }

        BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly).Run(rest, config);
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
