using VirtoCommerce.XCart.Benchmark;

// Thin runner over the un-extended XCart platform: the benchmark classes, fixtures, and the
// --baseline-src plumbing all live in VirtoCommerce.XCart.Benchmark.Core. BenchmarkEnvironment.Current
// defaults to the upstream setup defined there, so nothing else is wired here. A consuming module
// (XOrder, LEO) provides its own runner that sets BenchmarkEnvironment.Current before calling Run.
BenchmarkProgram.Run(args);
