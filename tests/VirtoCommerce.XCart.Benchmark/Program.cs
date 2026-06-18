using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// BenchmarkSwitcher forwards CLI args (--filter, --job, --anyCategories, --allCategories, ...) to
// BenchmarkDotNet. Always pass --filter when running non-interactively to avoid the interactive prompt.
// The categories column surfaces each benchmark's functional category (see Categories.cs) in the report.
var config = ManualConfig.Create(DefaultConfig.Instance).AddColumn(CategoriesColumn.Default);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
