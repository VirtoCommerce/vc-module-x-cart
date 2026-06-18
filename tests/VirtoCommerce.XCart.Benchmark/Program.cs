using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// BenchmarkSwitcher forwards CLI args (--filter, --job, --anyCategories, --allCategories, ...) to
// BenchmarkDotNet. Always pass --filter when running non-interactively to avoid the interactive prompt.
// The categories column surfaces each benchmark's functional category (see Categories.cs) in the report.
//
// Opt-in before/after comparison: `--baseline-src <path-to-src>` adds a second BenchmarkDotNet job
// ("before") that rebuilds XCart.Core/Data from <path> (a git worktree on the baseline revision) and
// runs it against the current source ("after"), yielding a Ratio column in a single process. The flag
// is parsed out here and never reaches BDN. When absent the config is unchanged — single default job,
// every existing flag (--filter / --anyCategories / --artifacts) behaves exactly as before.
// See README "Comparing before/after a change". Recommended with `--apples --job short`.
var (baselineSrc, rest) = ExtractOption(args, "--baseline-src");

var config = ManualConfig.Create(DefaultConfig.Instance).AddColumn(CategoriesColumn.Default);
if (baselineSrc is not null)
{
    config = config
        .AddJob(Job.Default.WithMsBuildArguments($"/p:XCartSrc={baselineSrc}").WithId("before").AsBaseline())
        .AddJob(Job.Default.WithId("after"));
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(rest, config);

// Removes "<name> <value>" from args and returns the value (null if the flag is absent), so the
// remaining args pass through to BenchmarkSwitcher untouched.
static (string, string[]) ExtractOption(string[] args, string name)
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
