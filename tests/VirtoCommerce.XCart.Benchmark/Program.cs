using BenchmarkDotNet.Running;

// BenchmarkSwitcher forwards CLI args (--filter, --job, --anyCategories, ...) to BenchmarkDotNet.
// Always pass --filter when running non-interactively to avoid the interactive selection prompt.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
