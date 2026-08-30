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
    /// benchmarks discovered in <paramref name="benchmarkAssembly"/>. Every other argument is forwarded
    /// to BenchmarkDotNet untouched; there is no runner dialect. The one exception is <c>--job</c> under
    /// <c>--baseline-src</c>, where it is consumed and applied to both paired jobs — forwarding it there
    /// would add a third, unpaired one, so the short <c>-j</c> spellings are rejected rather than passed on.
    /// README §"Comparing before/after a change" documents the comparison workflow.
    /// </summary>
    public static void Run(Assembly benchmarkAssembly, string[] args)
    {
        var (baselineSrc, rest) = ExtractOption(args, "--baseline-src");

        var config = ManualConfig.Create(DefaultConfig.Instance).AddColumn(CategoriesColumn.Default);
        if (baselineSrc is not null)
        {
            // before+after differ ONLY by source, so --job is consumed here rather than left for the
            // switcher — forwarding it would append a third, unpaired job. A spelling that slips past
            // the extraction costs twice over: Dry is selected below AND the leftover still reaches the
            // switcher, both silently. BenchmarkDotNet declares [Option('j', "job", ...)], so `-j Short`,
            // `-jShort` and `-j=Short` name the same flag without matching `--job`; reject those rather
            // than match them one at a time. A short bundle ending in j (`-mj Short`) still slips past —
            // widening the test to any `-…j…` would false-positive on values such as `-f*json*`.
            var (jobName, restAfterJob) = ExtractOption(rest, "--job");
            rest = restAfterJob;

            var leftoverJob = Array.Find(rest, x => x is not null
                && (x == "--job" || x.StartsWith("--job=", StringComparison.Ordinal) || x.StartsWith("-j", StringComparison.Ordinal)));
            if (leftoverJob is not null)
            {
                throw new ArgumentException(
                    "--baseline-src takes the job as `--job <Dry|Short|Default>` or `--job=<...>`; " +
                    $"'{leftoverJob}' would reach BenchmarkDotNet and add a third, unpaired job.");
            }

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

    // Removes "<name> <value>" OR "<name>=<value>" from args and returns the value (null if the flag is
    // absent), so the remaining args pass through to BenchmarkSwitcher untouched. Users write both
    // spellings because BenchmarkDotNet accepts both; narrowing this back to an exact-token match would
    // leave the equals form in `rest`, where the caller reads no value and falls back to its default
    // while BDN separately consumes the flag. Both halves of that are silent — see the leftover-job
    // refusal in Run, which is what covers the spellings this does not.
    private static (string, string[]) ExtractOption(string[] args, string name)
    {
        var prefix = name + "=";
        var index = Array.FindIndex(args, x => x is not null && (x == name || x.StartsWith(prefix, StringComparison.Ordinal)));
        if (index < 0)
        {
            return (null, args);
        }

        if (args[index].StartsWith(prefix, StringComparison.Ordinal))
        {
            var inlineValue = args[index][prefix.Length..];
            if (inlineValue.Length == 0)
            {
                throw new ArgumentException($"{name} requires a value.");
            }

            return (inlineValue, args.Where((_, i) => i != index).ToArray());
        }

        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{name} requires a value.");
        }

        var value = args[index + 1];
        var rest = args.Where((_, i) => i != index && i != index + 1).ToArray();

        return (value, rest);
    }
}
